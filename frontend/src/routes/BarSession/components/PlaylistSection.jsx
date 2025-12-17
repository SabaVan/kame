import React from 'react';
import '../styles/playlist.css';

const PlaylistSection = ({ playlist, currentSong, onSongClick, bidSubmitting }) => {
  if (!playlist) {
    return <p>No playlist loaded.</p>;
  }

  if (!playlist.songs || playlist.songs.length === 0) {
    return <p>Playlist is empty.</p>;
  }

  return (
    <div className="playlist-section">
      <h2>Current Playlist Songs</h2>
      <div className="playlist-container">
        {playlist.songs
          .filter((s) => !currentSong || s.id !== currentSong.id)
          .sort((a, b) => {
            const bidDiff = (b.currentBid || 0) - (a.currentBid || 0);
            if (bidDiff !== 0) return bidDiff;

            return (a.position || 0) - (b.position || 0);
          })
          .map((song) => (
            <div
              key={song.id}
              className="playlist-song-card clickable"
              onClick={() => onSongClick && onSongClick(song)}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') onSongClick && onSongClick(song);
              }}
            >
              <div className="song-info">
                <strong>{song.title}</strong> — {song.artist}
                {Number(song.currentBid) > 0 && (
                  <span className="current-bid">Current Bid: {song.currentBid} credits</span>
                )}
              </div>
              <div className="bid-hint">
                {bidSubmitting && bidSubmitting[song.id] ? 'Placing bid...' : 'Click to place bid'}
              </div>
            </div>
          ))}
      </div>
    </div>
  );
};

export default PlaylistSection;
