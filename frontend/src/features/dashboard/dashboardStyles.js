// =========================
// Container
// =========================
export const containerStyle = {
  padding: '20px',
  fontFamily: 'sans-serif',
  background: 'Canvas', // respects system background
  color: 'CanvasText',  // respects system text color
};

// =========================
// Box (user / song cards)
// =========================
export const boxStyle = (isDefault) => ({
  border: '1px solid color-mix(in srgb, CanvasText 15%, transparent)',
  borderRadius: '8px',
  padding: '12px',
  background: isDefault
    ? 'color-mix(in srgb, Canvas 92%, CanvasText 8%)' // default style
    : 'var(--profile-card-bg)',                        // regular style
  boxShadow: '0 1px 3px rgba(0,0,0,0.08)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  position: 'relative',
  cursor: 'pointer',
  transition: 'background 0.2s, border-color 0.15s',
});

// =========================
// Default bar box
// =========================
export const defaultBarBoxStyle = {
  border: '1px solid color-mix(in srgb, CanvasText 15%, transparent)',
  borderRadius: '12px',
  padding: '16px',
  marginBottom: '20px',
  background: 'var(--profile-info-bg)',
  boxShadow: '0 1px 3px rgba(0,0,0,0.08)',
};

// =========================
// Dot indicator
// =========================
export const dotStyle = (isOpen) => ({
  width: '10px',
  height: '10px',
  borderRadius: '50%',
  backgroundColor: isOpen ? '#4caf50' : 'gray',
  display: 'inline-block',
  marginRight: '8px',
});

// =========================
// Join / Leave button
// =========================
export const joinButtonStyle = (joined) => ({
  padding: '6px 12px',
  borderRadius: '6px',
  border: 'none',
  backgroundColor: joined ? '#dc2626' : '#2ecc71', // red/green like profile
  color: '#ffffff',
  cursor: 'pointer',
  transition: 'background 0.2s ease',
});

// =========================
// Tooltip
// =========================
export const tooltipStyle = {
  visibility: 'hidden',
  backgroundColor: 'rgba(0,0,0,0.75)',
  color: '#fff',
  textAlign: 'left',
  borderRadius: '6px',
  padding: '8px',
  position: 'absolute',
  zIndex: 10,
  top: '110%',
  left: '50%',
  transform: 'translateX(-50%)',
  width: '200px',
  fontSize: '0.85em',
  transition: 'visibility 0.2s, opacity 0.2s',
  opacity: 0,
  pointerEvents: 'none',
};

export const tooltipHoverStyle = {
  visibility: 'visible',
  opacity: 1,
};
