import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

export const useSignalRConnection = (barId) => {
  const [connection, setConnection] = useState(null);

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

  return connection;
};
