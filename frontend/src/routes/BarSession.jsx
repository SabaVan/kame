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

  // Fetch playlists for current bar
  const fetchPlaylist = useCallback(async () => {
    if (!barId) return;
    try {
      const res = await axios.get(`/api/playlists/bar/${barId}`, { withCredentials: true });
      const playlists = res.data;

      // For now, display the first playlist
      setPlaylist(playlists.length > 0 ? playlists[0] : null);
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

        // Initial load
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

  // Add song to playlist
  const handleAddSong = async (song) => {
    if (!playlist) return;

    try {
      await axios.post(
        `/api/playlists/${playlist.id}/add-song`,
        { ...song },
        { withCredentials: true }
      );
      // Refresh playlist immediately
      fetchPlaylist();
    } catch (err) {
      console.error('Failed to add song:', err);
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

          {searchResults.length > 0 && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {searchResults.map((song) => (
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
        <h2>Playlist</h2>
        {!playlist ? (
          <p>No playlist loaded.</p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {playlist.songs?.length ? (
              playlist.songs.map((song) => (
                <div
                  key={song.id}
                  style={{
                    padding: '8px',
                    background: '#f5f5f5',
                    borderRadius: '4px',
                  }}
                >
                  <strong>{song.title}</strong> — {song.artist}
                </div>
              ))
            ) : (
              <p>Playlist is empty.</p>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default BarSession;
