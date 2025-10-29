import React, { useState, useEffect } from 'react';

export default function Profile() {
  const [profile, setProfile] = useState(null);
  const [error, setError] = useState(null);

  const normalize = (raw) => {
    if (!raw) return { username: '', credits: 0 };

    // unwrap common wrappers
    const maybe = raw.user ?? raw.value ?? raw.profile ?? raw;

    const username = maybe.username ?? maybe.Username ?? maybe.userName ?? '';

    // resolve credits from several possible shapes
    let credits = 0;
    if (typeof maybe.credits === 'number') credits = maybe.credits;
    else if (maybe.credits && (typeof maybe.credits.amount === 'number' || typeof maybe.credits.Amount === 'number'))
      credits = maybe.credits.amount ?? maybe.credits.Amount;
    else if (typeof maybe.Credits === 'number') credits = maybe.Credits;
    else if (maybe.Credits && (typeof maybe.Credits.amount === 'number' || typeof maybe.Credits.Amount === 'number'))
      credits = maybe.Credits.amount ?? maybe.Credits.Amount;
    else credits = maybe.creditsAmount ?? maybe.balance ?? 0;

    return { username, credits };
  };

  useEffect(() => {
    const load = async () => {
      try {
        const res = await fetch('http://localhost:5023/api/users/profile', {
          credentials: 'include',
        });

        console.debug('Profile fetch status:', res.status);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);

        const text = await res.text();
        console.debug('Profile raw response text:', text);
        if (!text) throw new Error('Empty response from server');

        const data = JSON.parse(text);
        console.debug('Profile parsed JSON:', data);
        setProfile(normalize(data));
        return;
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
  if (!profile) return <div>Loading...</div>;

  return (
    <div className="profile-container" style={{ padding: '20px', maxWidth: '600px', margin: '0 auto' }}>
      <h2>Profile</h2>
      <div
        style={{
          backgroundColor: '#f5f5f5',
          padding: '20px',
          borderRadius: '8px',
          marginTop: '20px',
        }}
      >
        <p>
          <strong>Username:</strong> {profile.username}
        </p>
      </div>
    </div>
  );
}
