import { useEffect, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

export const useSignalRListeners = (connection, barId, onUsersUpdated, onPlaylistUpdated) => {
  useEffect(() => {
    if (!connection || !barId) return;

    let isMounted = true;

    const startConnection = async () => {
      if (connection.state === signalR.HubConnectionState.Connected) return;

      try {
        await connection.start();
        if (!isMounted) return;

        await connection.invoke('JoinBarGroup', barId);
        connection.on('BarUsersUpdated', onUsersUpdated);
        connection.on('PlaylistUpdated', onPlaylistUpdated);

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
      connection.off('BarUsersUpdated', onUsersUpdated);
      connection.off('PlaylistUpdated', onPlaylistUpdated);
      if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke('LeaveBarGroup', barId).catch(console.error);
        connection.stop().catch(console.error);
      }
    };
  }, [connection, barId, onUsersUpdated, onPlaylistUpdated]);
};
