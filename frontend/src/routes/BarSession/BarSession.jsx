import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useSignalRConnection, useSignalRListeners, usePlaylistData, useBarActions, useSongSearch } from './hooks';
import { Sidebar, CurrentSong, SearchSection, PlaylistSection, BidModal } from './components';
import './styles/barSession.css';
import './styles/bidModal.css';
import Loading from '@/components/Loading';

const BarSession = () => {
  const { barId } = useParams();
  const navigate = useNavigate();

  const [users, setUsers] = useState([]);
  const [playlist, setPlaylist] = useState(null);
  const [currentSong, setCurrentSong] = useState(null);
  const [loading, setLoading] = useState(true);
  const [bidSubmitting, setBidSubmitting] = useState({});

  const [modalVisible, setModalVisible] = useState(false);
  const [modalSong, setModalSong] = useState(null);

  const connection = useSignalRConnection(barId);
  const { fetchPlaylist, fetchCurrentSong } = usePlaylistData();
  const { fetchUsers, addSong, placeBid, leaveBar } = useBarActions();
  const { searchQuery, setSearchQuery, searchResults, searchLoading, handleSearch, getFilteredResults } =
    useSongSearch();

  const handleUsersUpdated = useCallback(async () => {
    const u = await fetchUsers(barId);
    setUsers(u || []);
  }, [fetchUsers, barId]);

  const handlePlaylistUpdated = useCallback(
    async (ev, payload) => {
      if (!payload) return;
      const { action, playlistId } = payload;

      if (action === 'song_started') {
        const song = await fetchCurrentSong(playlistId);
        setCurrentSong(song);
      } else if (action === 'song_ended') {
        const newCurrentSong = await fetchCurrentSong(playlistId);
        setCurrentSong(newCurrentSong);
        const updatedPlaylist = await fetchPlaylist(barId);
        setPlaylist(updatedPlaylist);
      } else if (action === 'bid_placed' || action === 'song_added') {
        const updated = await fetchPlaylist(barId);
        setPlaylist(updated);
      }
    },
    [fetchPlaylist, fetchCurrentSong, barId]
  );

  useSignalRListeners(connection, barId, handleUsersUpdated, handlePlaylistUpdated);

  useEffect(() => {
    const initialize = async () => {
      const [u, p] = await Promise.all([fetchUsers(barId), fetchPlaylist(barId)]);
      setUsers(u || []);
      setPlaylist(p);
      if (p) {
        const song = await fetchCurrentSong(p.id);
        setCurrentSong(song);
      }
      setLoading(false);
    };

    if (connection) initialize();
  }, [connection, fetchUsers, fetchPlaylist, fetchCurrentSong, barId]);

  const handleLeaveBar = async () => {
    try {
      await leaveBar(barId);
      if (connection) await connection.invoke('LeaveBarGroup', barId);
      navigate('/dashboard');
    } catch (err) {
      console.error('Failed to leave bar:', err);
    }
  };

  const handleAddSong = async (song) => {
    if (!playlist) return;
    try {
      await addSong(playlist.id, song);
      const updated = await fetchPlaylist(barId);
      setPlaylist(updated);
      // recompute remaining search results and close search if none left
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
    setBidSubmitting((p) => ({ ...p, [modalSong.id]: true }));
    try {
      await placeBid(playlist.id, modalSong.id, amount);
      const updated = await fetchPlaylist(barId);
      setPlaylist(updated);
      handleModalClose();
    } catch (err) {
      console.error('Failed to place bid:', err);
      alert(err.response?.data?.message || 'Failed to place bid.');
    } finally {
      setBidSubmitting((p) => ({ ...p, [modalSong.id]: false }));
    }
  };

  if (!barId) return <p>No bar selected.</p>;
  if (loading) return <Loading />;

  const filteredSearchResults = getFilteredResults(playlist?.songs || []);

  return (
    <div className="bar-session">
      <Sidebar users={users} onLeaveBar={handleLeaveBar} />

      <div className="main">
        <CurrentSong currentSong={currentSong} />

        <SearchSection
          searchQuery={searchQuery}
          setSearchQuery={setSearchQuery}
          searchLoading={searchLoading}
          onSearch={handleSearch}
          filteredResults={filteredSearchResults}
          onAddSong={handleAddSong}
        />

        <PlaylistSection
          playlist={playlist}
          currentSong={currentSong}
          onSongClick={handleSongClick}
          bidSubmitting={bidSubmitting}
        />

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
