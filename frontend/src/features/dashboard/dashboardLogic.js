import { useState, useEffect } from 'react';
import { getAllBars, getDefaultBar, joinBar, leaveBar } from './dashboardService';

export function useDashboard({ onJoin } = {}) {
  const [bars, setBars] = useState([]);
  const [defaultBar, setDefaultBar] = useState(null);
  const [loading, setLoading] = useState(true);
  const [joiningOrLeaving, setJoiningOrLeaving] = useState({});
  const [error, setError] = useState(null);
  const [joinedBars, setJoinedBars] = useState({}); // Track joined per bar

  useEffect(() => {
    async function fetchBars() {
      try {
        const [allBars, defaultBarData] = await Promise.all([getAllBars(), getDefaultBar()]);
        setBars(allBars);
        setDefaultBar(defaultBarData);

        // Check if user already joined each bar
        const joinedStatuses = {};
        await Promise.all(
          allBars.map(async (bar) => {
            const resp = await fetch(`/api/bar/${bar.id}/isJoined`);
            const isJoined = await resp.json();
            joinedStatuses[bar.id] = isJoined;
          })
        );
        setJoinedBars(joinedStatuses);
      } catch (err) {
        console.error(err);
        setError('Failed to load bars');
      } finally {
        setLoading(false);
      }
    }
    fetchBars();
  }, []);

const handleToggleJoin = async (barId) => {
  if (!barId || joiningOrLeaving[barId]) return;
  setJoiningOrLeaving((prev) => ({ ...prev, [barId]: true }));
  setError(null);

  const isJoining = !joinedBars[barId]; // true if user wants to join, false if leaving

  try {
    if (isJoining) {
      await joinBar(barId);
      setJoinedBars((prev) => ({ ...prev, [barId]: true }));
      if (onJoin) onJoin(barId);
    } else {
      await leaveBar(barId);
      setJoinedBars((prev) => ({ ...prev, [barId]: false }));
    }
  } catch (err) {
    console.error(err);
    setError(isJoining ? 'Failed to join the bar.' : 'Failed to leave the bar.');
  } finally {
    setJoiningOrLeaving((prev) => ({ ...prev, [barId]: false }));
  }
};


  const formatTimeLocal = (utcDateStr) => {
    if (!utcDateStr) return '';
    const d = new Date(utcDateStr);
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  const isBarOpen = (bar) => {
    if (!bar.openAtUtc || !bar.closeAtUtc) return false;

    const now = new Date();
    const nowUTCMinutes = now.getUTCHours() * 60 + now.getUTCMinutes();

    const openDate = new Date(bar.openAtUtc);
    const closeDate = new Date(bar.closeAtUtc);

    const openUTCMinutes = openDate.getUTCHours() * 60 + openDate.getUTCMinutes();
    const closeUTCMinutes = closeDate.getUTCHours() * 60 + closeDate.getUTCMinutes();

    if (openUTCMinutes < closeUTCMinutes) {
      return nowUTCMinutes >= openUTCMinutes && nowUTCMinutes < closeUTCMinutes;
    } else {
      // Overnight
      return nowUTCMinutes >= openUTCMinutes || nowUTCMinutes < closeUTCMinutes;
    }
  };

  return {
    bars,
    defaultBar,
    loading,
    joiningOrLeaving,
    error,
    joinedBars,
    handleToggleJoin,
    formatTimeLocal,
    isBarOpen,
  };
}
