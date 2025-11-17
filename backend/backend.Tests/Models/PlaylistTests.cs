using System;
using System.Linq;
using backend.Models;
using Xunit;

namespace backend.Tests.Models
{
    public class PlaylistTests
    {
        // Helper to create a Song with required properties
        private Song CreateSong(string title = "Song", string artist = "Test Artist", int durationSeconds = 3)
        {
            return new Song
            {
                Id = Guid.NewGuid(),
                Title = title,
                Artist = artist,
                Duration = TimeSpan.FromSeconds(durationSeconds)
            };
        }

        // Helper to create a PlaylistSong with required properties
        private PlaylistSong CreatePlaylistSong(Playlist playlist, Song song, Guid addedBy, int currentBid = 0, int position = 1)
        {
            return new PlaylistSong
            {
                PlaylistId = playlist.Id,
                Song = song,
                SongId = song.Id,
                AddedByUserId = addedBy,
                CurrentBid = currentBid,
                Position = position
            };
        }

        [Fact]
        public void AddSong_ShouldAddSongToPlaylist()
        {
            var playlist = new Playlist();
            var song = CreateSong();
            var userId = Guid.NewGuid();

            var addedSong = playlist.AddSong(song, userId);

            Assert.Contains(addedSong, playlist.Songs);
            Assert.Equal(playlist.Id, addedSong.PlaylistId);
            Assert.Equal(song.Id, addedSong.SongId);
            Assert.Equal(userId, addedSong.AddedByUserId);
        }

        [Fact]
        public void GetNextSong_ShouldReturnSong_WhenPlaylistHasSongs()
        {
            var playlist = new Playlist();
            var song1 = CreateSong("Song1");
            var song2 = CreateSong("Song2");
            var userId = Guid.NewGuid();

            playlist.AddSong(song1, userId);
            playlist.AddSong(song2, userId);

            var next = playlist.GetNextSong();

            Assert.NotNull(next);
            Assert.True(next == song1 || next == song2); // Could be highest bid first
        }

        [Fact]
        public void GetNextSong_ShouldReturnNull_WhenPlaylistEmpty()
        {
            var playlist = new Playlist();

            var next = playlist.GetNextSong();

            Assert.Null(next);
        }

        [Fact]
        public void RemoveSong_ShouldRemoveSongFromPlaylist()
        {
            var playlist = new Playlist();
            var song = CreateSong();
            var userId = Guid.NewGuid();
            var ps = playlist.AddSong(song, userId);

            playlist.RemoveSong(song.Id);

            Assert.DoesNotContain(ps, playlist.Songs);
        }

        [Fact]
        public void RemoveSong_ShouldResetCurrentlyPlayingIfRemoved()
        {
            var playlist = new Playlist();
            var song1 = CreateSong("Song1");
            var song2 = CreateSong("Song2");
            var userId = Guid.NewGuid();

            var ps1 = playlist.AddSong(song1, userId);
            var ps2 = playlist.AddSong(song2, userId);

            var nextSong = playlist.GetNextSong();
            playlist.RemoveSong(nextSong!.Id);

            var nextAfterRemove = playlist.GetNextSong();

            Assert.NotEqual(nextSong, nextAfterRemove);
        }

        [Fact]
        public void ReorderByBids_ShouldSortSongsAndUpdatePositions()
        {
            var playlist = new Playlist();
            var userId = Guid.NewGuid();

            var song1 = CreateSong("Song1");
            var song2 = CreateSong("Song2");
            var song3 = CreateSong("Song3");

            var ps1 = CreatePlaylistSong(playlist, song1, userId, currentBid: 5);
            var ps2 = CreatePlaylistSong(playlist, song2, userId, currentBid: 10);
            var ps3 = CreatePlaylistSong(playlist, song3, userId, currentBid: 7);

            playlist.Songs.AddRange(new[] { ps1, ps2, ps3 });

            playlist.ReorderByBids();

            Assert.Equal(ps2, playlist.Songs[0]);
            Assert.Equal(ps3, playlist.Songs[1]);
            Assert.Equal(ps1, playlist.Songs[2]);

            Assert.Equal(1, playlist.Songs[0].Position);
            Assert.Equal(2, playlist.Songs[1].Position);
            Assert.Equal(3, playlist.Songs[2].Position);
        }

        [Fact]
        public void Clear_ShouldRemoveAllSongsAndResetCurrentlyPlaying()
        {
            var playlist = new Playlist();
            var song = CreateSong();
            var userId = Guid.NewGuid();

            playlist.AddSong(song, userId);
            playlist.GetNextSong();

            playlist.Clear();

            Assert.Empty(playlist.Songs);
            var next = playlist.GetNextSong();
            Assert.Null(next);
        }
    }
}