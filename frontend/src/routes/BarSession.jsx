import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';
import BidModal from './BarSession/components/BidModal';
import './BarSession/styles/bidModal.css';
import '@styles/barSession.css';
import { API_URL } from '@/api/client';

const BarSession = () => {
  const { barId } = useParams();
  const navigate = useNavigate();

  const [users, setUsers] = useState([]);
  const [playlist, setPlaylist] = useState(null);
  const [currentSong, setCurrentSong] = useState(null);
  const [connection, setConnection] = useState(null);
  const [loading, setLoading] = useState(true);

  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState([]);
  const [bidSubmitting, setBidSubmitting] = useState({});
  const [modalVisible, setModalVisible] = useState(false);
  const [modalSong, setModalSong] = useState(null);

  // Helpers

  const sortPlaylistSongs = useCallback((pl) => {
    if (!pl?.songs) return pl;
    const sortedSongs = [...pl.songs].sort((a, b) => a.position - b.position);
    return { ...pl, songs: sortedSongs };
  }, []);

  // Fetching

  const fetchUsers = useCallback(async () => {
    if (!barId) return;
    try {
      const { data } = await axios.get(`${API_URL}/api/bar/${barId}/users`, { withCredentials: true });
      setUsers(data);
    } catch (err) {
      console.error('Failed to fetch users:', err);
      setUsers([]);
    } finally {
      setLoading(false);
    }
  }, [barId]);

  const fetchPlaylist = useCallback(async () => {
    if (!barId) return;
    try {
      const { data } = await axios.get(`${API_URL}/api/playlists/bar/${barId}?includeSongs=true`, {
        withCredentials: true,
      });
      const firstPlaylist = data[0];
      if (firstPlaylist) {
        if (!firstPlaylist.songs) {
          const songsRes = await axios.get(`${API_URL}/api/playlists/${firstPlaylist.id}/songs`, {
            withCredentials: true,
          });
          firstPlaylist.songs = songsRes.data;
        }
        setPlaylist(sortPlaylistSongs(firstPlaylist));
      } else {
        setPlaylist(null);
      }
    } catch (err) {
      console.error('Failed to fetch playlist:', err);
      setPlaylist(null);
    }
  }, [barId, sortPlaylistSongs]);

  // SignalR

  useEffect(() => {
    if (!barId) return;

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/bar`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);

    return () => {
      if (newConnection.state === signalR.HubConnectionState.Connected) {
        newConnection.invoke('LeaveBarGroup', barId).catch(console.error);
        newConnection.stop().catch(console.error);
      }
    };
  }, [barId]);

  useEffect(() => {
    if (!connection || !barId) return;

    let isMounted = true;

    const handleUsersUpdated = () => {
      if (isMounted) fetchUsers();
    };

    const handlePlaylistUpdated = async (eventPayload, payload) => {
      // Ignore the test payload (eventPayload), use the real payload
      if (!isMounted || !payload) return;

      const { action, songId, songTitle } = payload;

      if (action === 'song_started') {
        setCurrentSong({ id: songId, title: songTitle });
      } else if (action === 'song_ended' || action === 'bid_placed') {
        setCurrentSong(null);
        await fetchPlaylist(); // refresh playlist and sort by position
      }
    };

    const startConnection = async () => {
      if (connection.state === signalR.HubConnectionState.Connected) return;

      try {
        await connection.start();
        if (!isMounted) return;

        await connection.invoke('JoinBarGroup', barId);
        connection.on('BarUsersUpdated', handleUsersUpdated);
        connection.on('PlaylistUpdated', handlePlaylistUpdated);

        // Initial fetch
        await Promise.all([fetchUsers(), fetchPlaylist()]);

        connection.onclose((err) => {
          if (err) console.warn('SignalR disconnected:', err);
        });
      } catch (err) {
        console.error('SignalR connection failed:', err);
        setTimeout(() => startConnection(), 2000);
      }
    };

    startConnection();

    return () => {
      isMounted = false;
      connection.off('BarUsersUpdated', handleUsersUpdated);
      connection.off('PlaylistUpdated', handlePlaylistUpdated);
      if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke('LeaveBarGroup', barId).catch(console.error);
        connection.stop().catch(console.error);
      }
    };
  }, [connection, barId, fetchUsers, fetchPlaylist]);

  // Search / Add / Bid

  const performSearch = async (q) => {
    if (!q.trim()) return setSearchResults([]);
    try {
      const { data } = await axios.get(`${API_URL}/api/songs/search`, { params: { query: q, limit: 10 } });
      setSearchResults(data || []);
    } catch (err) {
      console.error('Failed to search songs:', err);
      setSearchResults([]);
    }
  };

  // Debounced auto-search when typing
  useEffect(() => {
    const q = searchQuery;
    const id = setTimeout(() => {
      performSearch(q);
    }, 300);
    return () => clearTimeout(id);
  }, [searchQuery]);

  const handleSearch = async (e) => {
    if (e && e.preventDefault) e.preventDefault();
    await performSearch(searchQuery);
  };

  const handleAddSong = async (song) => {
    if (!playlist) return;
    try {
      await axios.post(`${API_URL}/api/playlists/${playlist.id}/add-song`, song, { withCredentials: true });
      const updated = await fetchPlaylist();
      // recompute remaining search results and only clear if none remain
      const remaining = (searchResults || []).filter(
        (s) =>
          !updated?.songs?.some(
            (ps) =>
              ps.title.toLowerCase() === s.title.toLowerCase() && ps.artist.toLowerCase() === s.artist.toLowerCase()
          )
      );
      if (!remaining || remaining.length === 0) setSearchQuery('');
    } catch (err) {
      console.error('Failed to add song:', err);
    }
  };

  const handleSongClick = (song) => {
    setModalSong(song);
    setModalVisible(true);
  };

  const handleModalClose = () => {
    setModalVisible(false);
    setModalSong(null);
  };

  const handleModalSubmit = async (amount) => {
    if (!playlist || !modalSong) return;
    setBidSubmitting((prev) => ({ ...prev, [modalSong.id]: true }));
    try {
      await axios.post(
        `${API_URL}/api/playlists/${playlist.id}/bid`,
        { songId: modalSong.id, amount },
        { withCredentials: true }
      );
      await fetchPlaylist(); // fetch and sort after bid
      handleModalClose();
    } catch (err) {
      console.error('Failed to place bid:', err);
      alert(err.response?.data?.message || 'Failed to place bid.');
    } finally {
      setBidSubmitting((prev) => ({ ...prev, [modalSong.id]: false }));
    }
  };

  // Render

  const filteredSearchResults = searchResults.filter(
    (song) =>
      !playlist?.songs?.some(
        (s) =>
          s.title.toLowerCase() === song.title.toLowerCase() && s.artist.toLowerCase() === song.artist.toLowerCase()
      )
  );

  if (!barId) return <p>No bar selected.</p>;
  if (loading) return <p>Loading...</p>;

  return (
    <div className="bar-session">
      <div className="sidebar">
        <button
          className="leave-bar-btn"
          onClick={async () => {
            try {
              await axios.post(`${API_URL}/api/bar/${barId}/leave`, {}, { withCredentials: true });
              if (connection?.state === signalR.HubConnectionState.Connected)
                await connection.invoke('LeaveBarGroup', barId);
              navigate('/dashboard');
            } catch (err) {
              console.error('Failed to leave bar:', err);
            }
          }}
        >
          Leave Bar
        </button>

        <h3>Users</h3>
        <div className="users-list">
          {users.length === 0
            ? 'No users in this bar.'
            : users.map((u) => (
                <div key={u.id} className="user-card">
                  {u.username}
                </div>
              ))}
        </div>
      </div>

      <div className="main">
        <div style={{ marginBottom: '24px' }}>
          <h2>Currently Playing</h2>
          {currentSong ? (
            <div className="current-song-card">
              <strong>{currentSong.title}</strong>
            </div>
          ) : (
            <p>No song is playing.</p>
          )}
        </div>

        <div style={{ marginBottom: '24px' }}>
          <h2>Search Songs</h2>
          <form onSubmit={handleSearch} className="search-form">
            <input
              className="search-input"
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search songs..."
            />
          </form>
          {filteredSearchResults.length > 0 &&
            filteredSearchResults.map((song) => (
              <div key={song.id} className="search-result-card">
                <div>
                  <strong>{song.title}</strong> — {song.artist}
                </div>
                <button
                  className="add-song-btn"
                  onClick={() => {
                    handleAddSong(song);
                  }}
                >
                  Add
                </button>
              </div>
            ))}
        </div>

        <h2>Current Playlist Songs</h2>
        {!playlist ? (
          <p>No playlist loaded.</p>
        ) : playlist.songs?.length ? (
          <div className="playlist-container">
            {playlist.songs
              .filter((s) => !currentSong || s.id !== currentSong.id)
              .map((song) => (
                <div
                  key={song.id}
                  className="playlist-song-card clickable"
                  onClick={() => handleSongClick(song)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') handleSongClick(song);
                  }}
                >
                  <div>
                    <strong>{song.title}</strong> — {song.artist}
                    {Number(song.currentBid) > 0 && (
                      <span className="current-bid">Current Bid: {song.currentBid} credits</span>
                    )}
                  </div>
                  <div className="bid-hint">{bidSubmitting[song.id] ? 'Placing bid...' : 'Click to place bid'}</div>
                </div>
              ))}
          </div>
        ) : (
          <p>Playlist is empty.</p>
        )}
        <BidModal
          visible={modalVisible}
          song={modalSong}
          onClose={handleModalClose}
          onSubmit={handleModalSubmit}
          submitting={modalSong ? Boolean(bidSubmitting[modalSong.id]) : false}
          initialAmount={modalSong ? Number(modalSong.currentBid || 0) + 1 : 1}
        />
      </div>
    </div>
  );
};

export default BarSession;
