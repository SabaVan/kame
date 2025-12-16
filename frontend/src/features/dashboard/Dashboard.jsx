import React, { useState, useEffect } from 'react';
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

// Subcomponent for each bar card (with tooltip)
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

export default function Dashboard() {
  const navigate = useNavigate();
  const { bars, defaultBar, loading, error, joinedBars, handleToggleJoin, formatTimeLocal, isBarOpen } = useDashboard({
    onJoin: (barId) => navigate(`/bar/${barId}`),
  });

  const [selectedBar, setSelectedBar] = useState(null);

  // Set default bar as selected once it loads
  useEffect(() => {
    if (defaultBar) {
      setSelectedBar(defaultBar);
    }
  }, [defaultBar]);

  if (loading) return <p>Loading bars...</p>;

  return (
    <div style={containerStyle}>
      <h2 style={{ marginBottom: '16px' }}>Dashboard</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      {selectedBar && (
        <div style={defaultBarBoxStyle}>
          <div style={{ display: 'flex', alignItems: 'center', marginBottom: '8px' }}>
            <span style={dotStyle(isBarOpen(selectedBar))}></span>
            <span style={{ fontWeight: 'bold', fontSize: '1.1em' }}>{selectedBar.name}</span>
          </div>
          <div style={{ color: '#555', marginBottom: '10px' }}>
            Open: <b>{formatTimeLocal(selectedBar.openAtUtc)}</b> — Close:{' '}
            <b>{formatTimeLocal(selectedBar.closeAtUtc)}</b>
          </div>

          <button
            onClick={() => {
              const isJoined = joinedBars[selectedBar.id];
              if (isJoined) {
                navigate(`/bar/${selectedBar.id}`);
              } else {
                handleToggleJoin(selectedBar.id);
              }
            }}
            style={{
              ...joinButtonStyle(joinedBars[selectedBar.id]),
              width: '140px',
              minWidth: '140px',
              textAlign: 'center',
            }}
          >
            {joinedBars[selectedBar.id] ? 'Go to Session' : 'Join'}
          </button>
        </div>
      )}

      <h3 style={{ marginBottom: '12px' }}>All Bars</h3>

      {bars.length === 0 ? (
        <p>No bars found.</p>
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
            gap: '12px',
          }}
        >
          {bars.map((bar) => (
            <BarCard
              key={bar.id}
              bar={bar}
              isSelected={selectedBar?.id === bar.id}
              joined={joinedBars[bar.id]}
              isBarOpen={isBarOpen}
              formatTimeLocal={formatTimeLocal}
              onSelect={setSelectedBar}
            />
          ))}
        </div>
      )}
    </div>
  );
}
