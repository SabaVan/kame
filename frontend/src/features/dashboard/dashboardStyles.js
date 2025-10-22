export const containerStyle = { padding: '20px', fontFamily: 'sans-serif' };

export const boxStyle = (isDefault) => ({
  border: '1px solid #ccc',
  borderRadius: '10px',
  padding: '12px',
  background: isDefault ? '#eef7ff' : 'white',
  boxShadow: '0 1px 3px rgba(0,0,0,0.08)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  position: 'relative',
  cursor: 'pointer',
});

export const defaultBarBoxStyle = {
  border: '1px solid #ddd',
  borderRadius: '12px',
  padding: '16px',
  marginBottom: '20px',
  background: '#f9f9f9',
  boxShadow: '0 1px 3px rgba(0,0,0,0.08)',
};

export const dotStyle = (isOpen) => ({
  width: '10px',
  height: '10px',
  borderRadius: '50%',
  backgroundColor: isOpen ? 'green' : 'gray',
  display: 'inline-block',
  marginRight: '8px',
});

export const joinButtonStyle = (joined) => ({
  padding: '6px 12px',
  borderRadius: '6px',
  border: 'none',
  backgroundColor: joined ? '#e74c3c' : '#2ecc71',
  color: 'white',
  cursor: 'pointer',
});

export const tooltipStyle = {
  visibility: 'hidden',
  backgroundColor: 'rgba(0,0,0,0.75)',
  color: 'white',
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
