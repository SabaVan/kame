import React, { useState, useEffect } from 'react';
import Loading from '@/components/Loading';
import { useNavigate } from 'react-router-dom';
import { useDashboard } from './dashboardLogic';
import {
  containerStyle,
  boxStyle,
  defaultBarBoxStyle,
  dotStyle,
  joinButtonStyle,
  tooltipStyle,
  tooltipHoverStyle,
} from './dashboardStyles';

// Subcomponent for each bar card
function BarCard({ bar, isSelected, joined, isBarOpen, formatTimeLocal, onSelect }) {
  const [hovered, setHovered] = useState(false);
  const open = isBarOpen(bar);

  return (
    <div
      style={{ ...boxStyle(isSelected), position: 'relative', cursor: 'pointer' }}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      onClick={() => onSelect(bar)}
    >
      <div style={{ display: 'flex', alignItems: 'center' }}>
        <span style={dotStyle(open)}></span>
        <span style={{ fontWeight: 500 }}>{bar.name}</span>
      </div>

      {isSelected && joined && <span style={{ color: '#007bff', fontWeight: 'bold' }}>✓</span>}

      <div style={hovered ? { ...tooltipStyle, ...tooltipHoverStyle } : tooltipStyle}>
        <div>
          <b>Open:</b> {formatTimeLocal(bar.openAtUtc)}
        </div>
        <div>
          <b>Close:</b> {formatTimeLocal(bar.closeAtUtc)}
        </div>
        <div>
          <b>State:</b> {bar.state}
        </div>
        <div>
          <b>Current Playlist:</b> {bar.currentPlaylist || 'None'}
        </div>
      </div>
    </div>
  );
}

// ... (BarCard and imports remain the same)

export default function Dashboard() {
  const navigate = useNavigate();
  const isLoggedIn = sessionStorage.getItem('loggedIn') === 'true';

  const { bars, loading, error, joinedBars, handleToggleJoin, formatTimeLocal, isBarOpen } = useDashboard({
    onJoin: (barId) => navigate(`/bar/${barId}`),
  });

  const [selectedBar, setSelectedBar] = useState(null);

  // 1. INITIAL AUTO-SELECTION
  // This only runs once when bars are first loaded to pick the best "default"
  useEffect(() => {
    if (bars && bars.length > 0 && !selectedBar) {
      const firstOpenBar = bars.find((bar) => isBarOpen(bar));
      setSelectedBar(firstOpenBar || bars[0]);
    }
  }, [bars, isBarOpen, selectedBar]);

  const handleJoinAction = (barId) => {
    if (!isLoggedIn) {
      navigate('/login');
      return;
    }
    const isJoined = joinedBars[barId];
    if (isJoined) {
      navigate(`/bar/${barId}`);
    } else {
      handleToggleJoin(barId);
    }
  };

  if (loading) return <Loading />;

  return (
    <div style={containerStyle}>
      <h2 style={{ marginBottom: '16px' }}>Dashboard</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      {/* FEATURED SECTION: Updates whenever selectedBar changes */}
      {selectedBar && (
        <div style={defaultBarBoxStyle}>
          <div style={{ display: 'flex', alignItems: 'center', marginBottom: '8px' }}>
            <span style={dotStyle(isBarOpen(selectedBar))}></span>
            <span style={{ fontWeight: 'bold', fontSize: '1.2em' }}>{selectedBar.name}</span>
          </div>
          <p style={{ color: '#666', fontSize: '0.9rem', margin: '4px 0' }}>
            Local Time:{' '}
            <b>
              {formatTimeLocal(selectedBar.openAtUtc)} - {formatTimeLocal(selectedBar.closeAtUtc)}
            </b>
          </p>

          <button
            onClick={() => handleJoinAction(selectedBar.id)}
            style={{
              ...joinButtonStyle(isLoggedIn && joinedBars[selectedBar.id]),
              marginTop: '10px',
              padding: '10px 20px',
              cursor: 'pointer',
            }}
          >
            {!isLoggedIn ? 'Join the Party!' : joinedBars[selectedBar.id] ? 'Go to Session' : 'Join Bar'}
          </button>
        </div>
      )}

      <h3 style={{ marginBottom: '12px', marginTop: '30px' }}>Explore All Locations</h3>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
          gap: '15px',
        }}
      >
        {bars.map((bar) => (
          <BarCard
            key={bar.id}
            bar={bar}
            // If this bar is selected, we pass true to boxStyle to highlight it
            isSelected={selectedBar?.id === bar.id}
            joined={isLoggedIn && joinedBars[bar.id]}
            isBarOpen={isBarOpen}
            formatTimeLocal={formatTimeLocal}
            // This is the "switch" trigger!
            onSelect={(b) => {
              console.log('Switching to:', b.name);
              setSelectedBar(b);
            }}
          />
        ))}
      </div>
    </div>
  );
}
