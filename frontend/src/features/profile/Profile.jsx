import React, { useState, useEffect } from 'react';
import './Profile.css';
import Loading from '@/components/Loading';
import { API_URL } from '@/api/client';

export default function Profile() {
  const [profile, setProfile] = useState(null);
  const [error, setError] = useState(null);
  const [claimLoading, setClaimLoading] = useState(false);
  const [claimMessage, setClaimMessage] = useState('');

  const normalize = (raw) => {
    if (!raw) return { username: '', credits: 0 };

    // unwrap common wrappers
    const maybe = raw.user ?? raw.value ?? raw.profile ?? raw;

    const username = maybe.username ?? maybe.Username ?? maybe.userName ?? '';

    // resolve credits from several possible shapes
    let credits = 0;
    if (typeof maybe.credits === 'number') credits = maybe.credits;
    else if (
      maybe.credits &&
      (typeof maybe.credits.amount === 'number' ||
        typeof maybe.credits.Amount === 'number' ||
        typeof maybe.credits.total === 'number')
    )
      credits = maybe.credits.amount ?? maybe.credits.Amount ?? maybe.credits.total;
    else credits = maybe.creditsAmount ?? maybe.balance ?? 0;

    return { username, credits };
  };

  useEffect(() => {
    const load = async () => {
      try {
        const res = await fetch(`${API_URL}/api/users/profile`, {
          credentials: 'include',
        });
        console.debug('Profile fetch status:', res.status);

        const text = await res.text().catch(() => '');
        console.debug('Profile raw response text:', text);

        if (!res.ok) {
          // Try parse error info
          let parsedErr = null;
          try {
            parsedErr = text ? JSON.parse(text) : null;
          } catch (e) {
            /* ignore */
          }

          // If we have a cached profile, use it silently instead of showing raw 500
          const stored = localStorage.getItem('profile');
          if (stored) {
            try {
              const parsed = JSON.parse(stored);
              setProfile(normalize(parsed));
              return;
            } catch (e) {
              console.warn('Failed to parse stored profile', e);
            }
          }

          const msg =
            (parsedErr && (parsedErr.message || parsedErr.error)) || (text && text.slice(0, 200)) || 'Server error';
          setError(`Server unavailable: ${msg}`);
          return;
        }

        if (!text) {
          // empty but OK response — try cached profile
          const stored = localStorage.getItem('profile');
          if (stored) {
            try {
              const parsed = JSON.parse(stored);
              setProfile(normalize(parsed));
              return;
            } catch (e) {
              console.warn('Failed to parse stored profile', e);
            }
          }
          throw new Error('Empty response from server');
        }

        let data = null;
        try {
          data = JSON.parse(text);
        } catch (e) {
          console.warn('Failed to parse profile JSON:', e);
        }

        if (data) {
          console.debug('Profile parsed JSON:', data);
          setProfile(normalize(data));
          return;
        }
      } catch (err) {
        console.warn('Profile fetch failed:', err);

        const stored = localStorage.getItem('profile');
        if (stored) {
          try {
            const parsed = JSON.parse(stored);
            setProfile(normalize(parsed));
            return;
          } catch (e) {
            console.warn('Failed to parse stored profile', e);
          }
        }

        setError(err.message || 'Failed to load profile');
      }
    };

    load();
  }, []);

  if (error) return <div>Error: {error}</div>;
  if (!profile) return <Loading fullScreen />;
  const DAILY_AMOUNT = 25;

  const canClaim = profile.credits <= DAILY_AMOUNT;

  const claimDaily = async () => {
    setClaimMessage('');
    setClaimLoading(true);
    try {
      const res = await fetch(`${API_URL}/api/users/claim-daily`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
      });

      const text = await res.text();
      let data = null;
      try {
        data = text ? JSON.parse(text) : null;
      } catch (e) {
        /* ignore parse */
        e;
      }

      if (!res.ok) {
        const msg = (data && data.message) || `Failed: ${res.status}`;
        setClaimMessage(msg);
        setClaimLoading(false);
        return;
      }

      // success: update profile credits if returned
      if (data && typeof data.credits === 'number') {
        setProfile({ ...profile, credits: data.credits });
        setClaimMessage('Daily credits claimed successfully');
      } else {
        setClaimMessage('Daily credits claimed (no balance returned)');
      }
    } catch (err) {
      setClaimMessage(err.message || 'Request failed');
    } finally {
      setClaimLoading(false);
    }
  };

  return (
    <div className="profile-container">
      <div className="profile-card">
        <h2 className="profile-title">Your Profile</h2>

        <div className="profile-info">
          <p>
            <strong>Username:</strong> {profile.username}
          </p>
          <p>
            <strong>Credits:</strong> {profile.credits}
          </p>
        </div>

        <button
          className={`claim-btn ${!canClaim || claimLoading ? 'disabled' : ''}`}
          onClick={claimDaily}
          disabled={!canClaim || claimLoading}
        >
          {claimLoading ? 'Claiming...' : `Claim Daily Bonus (+${DAILY_AMOUNT})`}
        </button>

        {!canClaim && <div className="warning-text">Cannot claim when balance is greater than {DAILY_AMOUNT}.</div>}

        {claimMessage && <div className="claim-message">{claimMessage}</div>}
      </div>
    </div>
  );
}
