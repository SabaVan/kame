import axios from 'axios';
import { useCallback } from 'react';
import { API_URL } from '@/api/client';

export const useBarActions = () => {
  const fetchUsers = useCallback(async (barId) => {
    if (!barId) return [];
    try {
      const { data } = await axios.get(`${API_URL}/api/bar/${barId}/users`, { withCredentials: true });
      return data;
    } catch (err) {
      console.error('Failed to fetch users:', err);
      return [];
    }
  }, []);

  const addSong = useCallback(async (playlistId, song) => {
    try {
      await axios.post(`${API_URL}/api/playlists/${playlistId}/add-song`, song, {
        withCredentials: true,
      });
      return true;
    } catch (err) {
      console.error('Failed to add song:', err);
      return false;
    }
  }, []);

  const placeBid = useCallback(async (playlistId, songId, amount) => {
    try {
      await axios.post(
        `${API_URL}/api/playlists/${playlistId}/bid`,
        { songId, amount },
        {
          withCredentials: true,
        }
      );
      return true;
    } catch (err) {
      console.error('Failed to place bid:', err);
      throw err;
    }
  }, []);

  const leaveBar = useCallback(async (barId) => {
    try {
      await axios.post(`${API_URL}/api/bar/${barId}/leave`, {}, { withCredentials: true });
      return true;
    } catch (err) {
      console.error('Failed to leave bar:', err);
      return false;
    }
  }, []);

  return { fetchUsers, addSong, placeBid, leaveBar };
};
