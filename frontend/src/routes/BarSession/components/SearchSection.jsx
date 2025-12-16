import React from 'react';
import '../styles/searchSection.css';

const SearchSection = ({ searchQuery, setSearchQuery, searchLoading, onSearch, filteredResults, onAddSong }) => {
  return (
    <div className="search-section">
      <h2>Search Songs</h2>
      <form onSubmit={onSearch} className="search-form">
        <input
          className="search-input"
          type="text"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder="Search songs..."
        />
      </form>

      <div className="search-results">
        {searchLoading && <div className="search-loading">Searching...</div>}

        {!searchLoading && filteredResults.length === 0 && searchQuery.trim() !== '' && (
          <div className="search-empty">No results found.</div>
        )}

        {!searchLoading &&
          filteredResults.length > 0 &&
          filteredResults.map((song) => (
            <div key={song.id} className="search-result-card">
              <div>
                <strong>{song.title}</strong> — {song.artist}
              </div>
              <button className="add-song-btn" onClick={() => onAddSong(song)}>
                Add
              </button>
            </div>
          ))}
      </div>
    </div>
  );
};

export default SearchSection;
