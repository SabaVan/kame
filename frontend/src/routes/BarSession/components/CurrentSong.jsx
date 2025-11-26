import React from 'react';
import '../styles/currentSong.css';

const CurrentSong = ({ currentSong }) => {
  return (
    <div className="current-song-section">
      <h2>Currently Playing</h2>
      {currentSong ? (
        <div className="current-song-card">
          <strong>{currentSong.title}</strong>
        </div>
      ) : (
        <p>No song is playing.</p>
      )}
    </div>
  );
};

export default CurrentSong;
