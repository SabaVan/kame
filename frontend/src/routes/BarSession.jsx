import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';

const BarSession = () => {
  const { barId } = useParams();
  const navigate = useNavigate();

  const [users, setUsers] = useState([]);
  const [playlist, setPlaylist] = useState(null);
  const [connection, setConnection] = useState(null);
  const [loading, setLoading] = useState(true);

  // Song search
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState([]);
  const [searchLoading, setSearchLoading] = useState(false);

  // Bidding input state
  const [bidAmounts, setBidAmounts] = useState({}); // key: songId, value: bid amount
  const [bidSubmitting, setBidSubmitting] = useState({}); // key: songId, value: boolean

  // Fetch users
  const fetchUsers = useCallback(async () => {
    if (!barId) return;
    try {
      const response = await axios.get(`/api/bar/${barId}/users`, { withCredentials: true });
      setUsers(response.data);
    } catch (err) {
      console.error('Failed to fetch users:', err);
      setUsers([]);
    } finally {
      setLoading(false);
    }
  }, [barId]);

  // Fetch playlist
  const fetchPlaylist = useCallback(async () => {
    if (!barId) return;
    try {
      const res = await axios.get(`/api/playlists/bar/${barId}?includeSongs=true`, { withCredentials: true });
      const playlists = res.data;

      if (playlists.length > 0) {
        const firstPlaylist = playlists[0];

        if (!firstPlaylist.songs) {
          const songsRes = await axios.get(`/api/playlists/${firstPlaylist.id}/songs`, { withCredentials: true });
          firstPlaylist.songs = songsRes.data;
        }

        setPlaylist(firstPlaylist);
      } else {
        setPlaylist(null);
      }
    } catch (err) {
      console.error('Failed to fetch playlist:', err);
      setPlaylist(null);
    }
  }, [barId]);

  // Initialize SignalR
  useEffect(() => {
    if (!barId) return;

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/bar', { withCredentials: true })
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

  // Connect & subscribe to updates
  useEffect(() => {
    if (!connection || !barId) return;
    let isMounted = true;

    const startConnection = async () => {
      try {
        await connection.start();
        if (!isMounted) return;

        await connection.invoke('JoinBarGroup', barId);
        connection.on('BarUsersUpdated', fetchUsers);
        connection.on('PlaylistUpdated', fetchPlaylist);

        await Promise.all([fetchUsers(), fetchPlaylist()]);

        connection.onclose((err) => console.warn('SignalR disconnected:', err));
      } catch (err) {
        console.error('SignalR connection failed:', err);
        setTimeout(startConnection, 2000);
      }
    };

    startConnection();

    return () => {
      isMounted = false;
      connection.off('BarUsersUpdated', fetchUsers);
      connection.off('PlaylistUpdated', fetchPlaylist);
    };
  }, [connection, barId, fetchUsers, fetchPlaylist]);

  // Handle search
  const handleSearch = async (e) => {
    e.preventDefault();
    if (!searchQuery.trim()) {
      setSearchResults([]);
      return;
    }

    setSearchLoading(true);
    try {
      const res = await axios.get(`/api/songs/search`, {
        params: { query: searchQuery, limit: 10 },
      });
      setSearchResults(res.data);
    } catch (err) {
      console.error('Failed to search songs:', err);
      setSearchResults([]);
    } finally {
      setSearchLoading(false);
    }
  };

  // Filter search results to exclude already added songs
  const filteredSearchResults = searchResults.filter(
    (song) =>
      !playlist?.songs?.some(
        (s) =>
          s.title.toLowerCase() === song.title.toLowerCase() && s.artist.toLowerCase() === song.artist.toLowerCase()
      )
  );

  // Add song to playlist
  const handleAddSong = async (song) => {
    if (!playlist) return;
    try {
      await axios.post(`/api/playlists/${playlist.id}/add-song`, { ...song }, { withCredentials: true });
      fetchPlaylist();
    } catch (err) {
      console.error('Failed to add song:', err);
    }
  };

  // Bid on song
  const handleBid = async (songId) => {
    const amount = parseInt(bidAmounts[songId], 10);
    if (isNaN(amount) || amount <= 0) {
      alert('Enter a valid bid amount.');
      return;
    }

    setBidSubmitting((prev) => ({ ...prev, [songId]: true }));

    try {
      await axios.post(`/api/playlists/${playlist.id}/bid`, { songId, amount }, { withCredentials: true });
      setBidAmounts((prev) => ({ ...prev, [songId]: '' }));
    } catch (err) {
      console.error('Failed to place bid:', err);
      alert(err.response?.data?.message || 'Failed to place bid.');
      console.error('Bid error full response:', err.response);
    } finally {
      setBidSubmitting((prev) => ({ ...prev, [songId]: false }));
    }
  };

  if (!barId) return <p>No bar selected.</p>;
  if (loading) return <p>Loading...</p>;

  return (
    <div style={{ display: 'flex', height: '100vh', gap: '16px' }}>
      {/* Sidebar */}
      <div
        style={{
          width: '220px',
          padding: '16px',
          borderRight: '1px solid #ccc',
          display: 'flex',
          flexDirection: 'column',
          gap: '12px',
        }}
      >
        <button
          onClick={async () => {
            try {
              await axios.post(`/api/bar/${barId}/leave`, {}, { withCredentials: true });
              if (connection?.state === signalR.HubConnectionState.Connected) {
                await connection.invoke('LeaveBarGroup', barId);
              }
              navigate('/dashboard');
            } catch (err) {
              console.error('Failed to leave bar:', err);
            }
          }}
          style={{
            padding: '6px 10px',
            background: '#e74c3c',
            color: '#fff',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '0.85rem',
          }}
        >
          Leave Bar
        </button>

        <h3 style={{ marginTop: '16px', marginBottom: '8px' }}>Users</h3>
        <div style={{ flex: 1, overflowY: 'auto' }}>
          {users.length === 0 ? (
            <div>No users in this bar.</div>
          ) : (
            users.map((user) => (
              <div
                key={user.id}
                style={{
                  padding: '6px',
                  background: '#f5f5f5',
                  borderRadius: '4px',
                  textAlign: 'center',
                  fontSize: '0.9rem',
                  marginBottom: '4px',
                }}
              >
                {user.username}
              </div>
            ))
          )}
        </div>
      </div>

      {/* Main */}
      <div style={{ flex: 1, padding: '16px', overflowY: 'auto' }}>
        {/* Search Songs */}
        <div style={{ marginBottom: '24px' }}>
          <h2>Search Songs</h2>
          <form onSubmit={handleSearch} style={{ display: 'flex', gap: '8px', marginBottom: '12px' }}>
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search songs..."
              style={{
                flex: 1,
                padding: '8px',
                border: '1px solid #ccc',
                borderRadius: '4px',
              }}
            />
            <button
              type="submit"
              style={{
                padding: '8px 12px',
                background: '#3498db',
                color: '#fff',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              {searchLoading ? 'Searching...' : 'Search'}
            </button>
          </form>

          {filteredSearchResults.length > 0 && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {filteredSearchResults.map((song) => (
                <div
                  key={song.id}
                  style={{
                    padding: '8px',
                    background: '#f5f5f5',
                    borderRadius: '4px',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                  }}
                >
                  <div>
                    <strong>{song.title}</strong> — {song.artist}
                  </div>
                  <button
                    onClick={() => handleAddSong(song)}
                    style={{
                      padding: '4px 8px',
                      background: '#2ecc71',
                      color: '#fff',
                      border: 'none',
                      borderRadius: '4px',
                      cursor: 'pointer',
                      fontSize: '0.8rem',
                    }}
                  >
                    Add
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Playlist Section */}
        <h2>Current Playlist Songs</h2>
        {!playlist ? (
          <p>No playlist loaded.</p>
        ) : playlist.songs?.length ? (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {playlist.songs
              .slice()
              .sort((a, b) => {
                if (b.currentBid !== a.currentBid) {
                  return b.currentBid - a.currentBid;
                }
                return a.position - b.position;
              })
              .map((song) => (
                <div
                  key={song.id}
                  style={{
                    padding: '8px',
                    background: '#f5f5f5',
                    borderRadius: '4px',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                  }}
                >
                  <div>
                    <strong>{song.title}</strong> — {song.artist}
                    {Number(song.currentBid) > 0 && (
                      <span style={{ marginLeft: '8px', fontSize: '0.8rem', color: '#555' }}>
                        Current Bid: {song.currentBid} credits
                      </span>
                    )}
                  </div>
                  <div style={{ display: 'flex', gap: '4px', alignItems: 'center' }}>
                    <input
                      type="number"
                      min="1"
                      placeholder="Bid"
                      value={bidAmounts[song.id] || ''}
                      onChange={(e) => setBidAmounts((prev) => ({ ...prev, [song.id]: e.target.value }))}
                      style={{ width: '60px', padding: '4px', borderRadius: '4px', border: '1px solid #ccc' }}
                    />
                    <button
                      onClick={() => handleBid(song.id)}
                      disabled={bidSubmitting[song.id]}
                      style={{
                        padding: '4px 8px',
                        background: '#f39c12',
                        color: '#fff',
                        border: 'none',
                        borderRadius: '4px',
                        cursor: 'pointer',
                        fontSize: '0.8rem',
                      }}
                    >
                      {bidSubmitting[song.id] ? 'Bidding...' : 'Bid'}
                    </button>
                  </div>
                </div>
              ))}
          </div>
        ) : (
          <p>Playlist is empty.</p>
        )}
      </div>
    </div>
  );
};

export default BarSession;
