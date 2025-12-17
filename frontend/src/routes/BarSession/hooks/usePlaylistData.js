import axios from 'axios';
import { useCallback } from 'react';
import { API_URL } from '@/api/client';

export const usePlaylistData = () => {
  const sortPlaylistSongs = useCallback((pl) => {
    if (!pl?.songs) return pl;
    const sortedSongs = [...pl.songs].sort((a, b) => a.position - b.position);
    return { ...pl, songs: sortedSongs };
  }, []);

  const fetchPlaylist = useCallback(
    async (barId) => {
      if (!barId) return null;
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
          return sortPlaylistSongs(firstPlaylist);
        }
        return null;
      } catch (err) {
        console.error('Failed to fetch playlist:', err);
        return null;
      }
    },
    [sortPlaylistSongs]
  );

  const fetchCurrentSong = useCallback(async (playlistId) => {
    if (!playlistId) return null;
    try {
      const { data } = await axios.get(`${API_URL}/api/playlists/${playlistId}/current-song`, {
        withCredentials: true,
      });
      return data;
    } catch (err) {
      console.error('Failed to fetch current song:', err);
      return null;
    }
  }, []);

  return { fetchPlaylist, sortPlaylistSongs, fetchCurrentSong };
};
