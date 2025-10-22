import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import axios from 'axios';

const BarSession = () => {
  const { barId } = useParams();
  const navigate = useNavigate();
  const [users, setUsers] = useState([]);
  const [connection, setConnection] = useState(null);
  const [loading, setLoading] = useState(true);

  // ✅ useCallback ensures stable reference across renders
  const fetchUsers = useCallback(async () => {
    if (!barId) return;
    try {
      const response = await axios.get(`/api/bar/${barId}/users`, { withCredentials: true });
      setUsers(response.data);
      setLoading(false);
    } catch (err) {
      console.error('Failed to fetch users:', err);
      setUsers([]);
      setLoading(false);
    }
  }, [barId]);

  // Initialize SignalR connection
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
        setUsers([]);
        newConnection.stop().catch(console.error);
      }
    };
  }, [barId]);

  // Start connection & subscribe to updates
  useEffect(() => {
    if (!connection || !barId) return;

    let isMounted = true;

    const startConnection = async () => {
      try {
        await connection.start();
        if (!isMounted) return;

        await connection.invoke('JoinBarGroup', barId);
        connection.on('BarUsersUpdated', fetchUsers);
        fetchUsers();
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
    };
  }, [connection, barId, fetchUsers]);

  if (!barId) return <p>No bar selected.</p>;
  if (loading) return <p>Loading users...</p>;

  return (
    <div style={{ display: 'flex', height: '100vh', gap: '16px' }}>
      {/* Left sidebar: buttons + users */}
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
        {/* Tab / Button Panel */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          <button
            onClick={async () => {
              try {
                await axios.post(`/api/bar/${barId}/leave`, {}, { withCredentials: true });
                if (connection?.state === signalR.HubConnectionState.Connected) {
                  await connection.invoke('LeaveBarGroup', barId);
                }
                setUsers([]);
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
        </div>

        {/* Users list */}
        <h3 style={{ marginTop: '16px', marginBottom: '8px' }}>Users</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', overflowY: 'auto', flex: 1 }}>
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
                }}
              >
                {user.username}
              </div>
            ))
          )}
        </div>
      </div>

      {/* Main content: playlist placeholder */}
      <div style={{ flex: 1, padding: '16px' }}>
        <h2>Playlist</h2>
        <p>Here will go the playlist, now left space for future UI.</p>
      </div>
    </div>
  );
};

export default BarSession;
