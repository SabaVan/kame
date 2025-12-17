import { useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

export const useSignalRListeners = (connection, barId, onUsersUpdated, onPlaylistUpdated, setSignalrState) => {
  useEffect(() => {
    if (!connection || !barId) return;

    let isMounted = true;

    const startConnection = async () => {
      // If already connecting or connected, don't try to start again
      if (connection.state !== signalR.HubConnectionState.Disconnected) return;

      try {
        await connection.start();
        if (!isMounted) return;

        console.log('SignalR Connected!');
        setSignalrState('Connected');

        connection.on('BarUsersUpdated', onUsersUpdated);
        connection.on('PlaylistUpdated', onPlaylistUpdated);
      } catch (err) {
        console.error('SignalR connection failed:', err);
        // Only retry if we are still on the page
        if (isMounted) setTimeout(() => startConnection(), 5000);
      }
    };

    startConnection();

    return () => {
      isMounted = false;
      // Remove listeners so they don't stack up
      connection.off('BarUsersUpdated');
      connection.off('PlaylistUpdated');
    };
  }, [connection, barId, onUsersUpdated, onPlaylistUpdated, setSignalrState]);
};
