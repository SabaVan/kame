import axios from 'axios';
import { useState, useCallback, useEffect, useRef } from 'react';
import { API_URL } from '@/api/client';

export const useSongSearch = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const abortRef = useRef(null);
  const clearSearch = useCallback(() => {
    if (abortRef.current) {
      abortRef.current.abort(); // <–– cancel any active request
    }
    setSearchQuery('');
    setSearchResults([]);
  }, []);

  const performSearch = useCallback(async (query) => {
    if (!query.trim()) {
      setSearchResults([]);
      return;
    }

    setSearchLoading(true);
    try {
      if (abortRef.current) {
        abortRef.current.abort();
      }
      abortRef.current = new AbortController();
      const { data } = await axios.get(`${API_URL}/api/songs/search`, {
        params: { query, limit: 10 },
        signal: abortRef.current.signal,
      });
      setSearchResults(data || []);
    } catch (err) {
      if (err.name === 'CanceledError' || err.name === 'AbortError') return;
      console.error('Failed to search songs:', err);
      setSearchResults([]);
    } finally {
      setSearchLoading(false);
      abortRef.current = null;
    }
  }, []);

  // Debounced auto-search when query changes
  useEffect(() => {
    const q = searchQuery;
    const id = setTimeout(() => {
      performSearch(q);
    }, 300);

    return () => clearTimeout(id);
  }, [searchQuery, performSearch]);

  // Exposed handler for form submit to run immediate search
  const handleSearch = useCallback(
    async (e) => {
      if (e && e.preventDefault) e.preventDefault();
      await performSearch(searchQuery);
    },
    [performSearch, searchQuery]
  );

  const getFilteredResults = useCallback(
    (playlistSongs) => {
      return searchResults.filter(
        (song) =>
          !playlistSongs?.some(
            (s) =>
              s.title.toLowerCase() === song.title.toLowerCase() && s.artist.toLowerCase() === song.artist.toLowerCase()
          )
      );
    },
    [searchResults]
  );

  return {
    searchQuery,
    setSearchQuery,
    searchResults,
    searchLoading,
    handleSearch,
    getFilteredResults,
    clearSearch,
  };
};
