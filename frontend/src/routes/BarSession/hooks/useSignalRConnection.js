import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { API_URL } from '@/api/client';

export const useSignalRConnection = (barId) => {
  const [connection, setConnection] = useState(null);
  const [signalrState, setSignalrState] = useState('Disconnected');

  useEffect(() => {
    if (!barId) return;

    // Create the connection ONCE
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/bar`, { withCredentials: true })
      .withAutomaticReconnect([0, 2000, 5000, 10000]) // Custom retry intervals
      .build();

    const updateState = () => setSignalrState(newConnection.state);

    newConnection.onreconnecting(updateState);
    newConnection.onreconnected(updateState);
    newConnection.onclose(updateState);

    setConnection(newConnection);

    return () => {
      // Only stop if it's actually connected/connecting
      if (newConnection.state !== signalR.HubConnectionState.Disconnected) {
        newConnection.stop().catch(() => {}); // Ignore errors on stop
      }
    };
  }, [barId]);

  return { connection, signalrState, setSignalrState };
};
