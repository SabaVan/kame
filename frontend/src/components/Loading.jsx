import React from 'react';
import './Loading.css';

export default function Loading({ fullScreen = false, label = 'Loading' }) {
  return (
    <div className={fullScreen ? 'loading-fullscreen' : 'loading-container'} aria-live="polite">
      <div className="loading-spinner" role="status" aria-label={label} />
    </div>
  );
}
