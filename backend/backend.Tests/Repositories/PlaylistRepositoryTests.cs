using System;
using backend.Models;
using backend.Shared.Enums;
using backend.Common;
using Xunit;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using Microsoft.AspNetCore.Components.Web;

namespace backend.Tests.Repositories
{
    public class PlaylistRepositoryTests
    {
        private PlaylistRepository CreateRepositoryWithFakeDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "PlaylistTestDb")
                .Options;

            var db = new AppDbContext(options);

            return new PlaylistRepository(db);
        }

        [Fact]
        public async Task AddAsync_SavesPlaylist_Successfully()
        {
            // Arrange
            var playlistRepository = CreateRepositoryWithFakeDb();
            var id = Guid.NewGuid();

            var playlist = new Playlist { Id = id };

            // Act
            await playlistRepository.AddAsync(playlist);

            // Assert
            var fromDb = await playlistRepository.GetByIdAsync(id);
            Assert.NotNull(fromDb);
            Assert.Equal(id, fromDb.Id);
        }

        [Fact]
        public async Task GetPlaylistSongBySongIdAsync_WhenOnePlaylistIsPresent_ReturnsCorrectPlaylistSong()
        {
            var playlistRepository = CreateRepositoryWithFakeDb();

            // Arrange
            var playlist1 = new Playlist();
            // Seed songs
            var song1 = new Song { Title = "Song 1", Artist = "Artist 1" };
            var song2 = new Song { Title = "Song 2", Artist = "Artist 2" };

            // create tmp user GID
            var userGUID = Guid.NewGuid();

            playlist1.AddSong(song1, userGUID);
            playlist1.AddSong(song2, userGUID);

            await playlistRepository.AddAsync(playlist1);

            // Act
            var result = await playlistRepository.GetPlaylistSongBySongIdAsync(song2.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(song2.Id, result.SongId);
            Assert.Equal("Song 2", result.Song.Title);
        }

        [Fact]
        public async Task GetPlaylistSongBySongIdAsync_WhenOnePlaylistIsPresentAndThereIsNoSong_ReturnsNull()
        {
            var playlistRepository = CreateRepositoryWithFakeDb();

            // Arrange
            var playlist1 = new Playlist();
            // Seed songs
            var song1 = new Song { Title = "Song 1", Artist = "Artist 1" };
            var song2 = new Song { Title = "Song 2", Artist = "Artist 2" };

            // create tmp user GID
            var userGUID = Guid.NewGuid();

            playlist1.AddSong(song1, userGUID);

            await playlistRepository.AddAsync(playlist1);

            // Act
            var result = await playlistRepository.GetPlaylistSongBySongIdAsync(song2.Id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPlaylistSongBySongIdAsync_TwoPlaylistsArePresentAndThereIsNoSong_ReturnsNull()
        {
            var playlistRepository = CreateRepositoryWithFakeDb();

            // Arrange
            var playlist1 = new Playlist();
            var playlist2 = new Playlist();
            // Seed songs
            var song1 = new Song { Title = "Song 1", Artist = "Artist 1" };
            var song2 = new Song { Title = "Song 2", Artist = "Artist 2" };

            // create tmp user GID
            var userGUID = Guid.NewGuid();

            playlist1.AddSong(song1, userGUID);

            await playlistRepository.AddAsync(playlist1);
            await playlistRepository.AddAsync(playlist2);

            // Act
            var result = await playlistRepository.GetPlaylistSongBySongIdAsync(song2.Id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPlaylistSongBySongIdAsync_TwoPlaylistsArePresentAndThereIsSong_ReturnsCorrectPlaylistSong()
        {
            var playlistRepository = CreateRepositoryWithFakeDb();

            // Arrange
            var playlist1 = new Playlist();
            var playlist2 = new Playlist();
            // Seed songs
            var song1 = new Song { Title = "Song 1", Artist = "Artist 1" };
            var song2 = new Song { Title = "Song 2", Artist = "Artist 2" };

            // create tmp user GID
            var userGUID = Guid.NewGuid();

            playlist1.AddSong(song1, userGUID);
            playlist2.AddSong(song2, userGUID);

            await playlistRepository.AddAsync(playlist1);
            await playlistRepository.AddAsync(playlist2);

            // Act
            var result = await playlistRepository.GetPlaylistSongBySongIdAsync(song2.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(song2.Id, result.SongId);
            Assert.Equal("Song 2", result.Song.Title);
        }

        [Fact]
        public async Task GetByIdAsync_ThereAreNoPlaylists_ReturnsNull()
        {
            var playlistRepository = CreateRepositoryWithFakeDb();

            // Act
            var result = await playlistRepository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ThereIsPlaylist_ReturnsPlaylist()
        {
            var playlistRepository = CreateRepositoryWithFakeDb();

            // Arrange
            var playlist = new Playlist();
            await playlistRepository.AddAsync(playlist);

            // Act
            var result = await playlistRepository.GetByIdAsync(playlist.Id);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AddSongAsync_AddSong_SucessfullyAdded()
        {
            // Arrange
            var playlistRepository = CreateRepositoryWithFakeDb();
            var songGUID = Guid.NewGuid();
            var songTitle = "Song 1";
            var songArtist = "Artist 1";

            var song = new Song { Id = songGUID, Title = songTitle, Artist = songArtist };

            // Act
            await playlistRepository.AddSongAsync(song);

            // Assert
            var result = await playlistRepository.GetSongByIdAsync(song.Id);
            Assert.NotNull(result);
            Assert.Equal(songGUID, result.Id);
            Assert.Equal(songTitle, result.Title);
            Assert.Equal(songArtist, result.Artist);
        }

        [Fact]
        public async Task AddPlaylistSongAsync_AddedOnePlaylistSong_SucessfullyAdded()
        {
            // Arrange
            var playlistRepository = CreateRepositoryWithFakeDb();
            var playlistSongGUID = Guid.NewGuid();
            var userGUID = Guid.NewGuid();
            var addedAt = DateTime.UtcNow;

            var song = new Song { Id = Guid.NewGuid(), Title = "Song 1", Artist = "Artist 1" };
            var playlistSong = new PlaylistSong
            {
                PlaylistId = playlistSongGUID,
                SongId = song.Id,
                Song = song,
                AddedByUserId = userGUID,
                AddedAt = addedAt
            };

            // Act
            await playlistRepository.AddPlaylistSongAsync(playlistSong);

            // Assert
            var result = await playlistRepository.GetPlaylistSongBySongIdAsync(song.Id);
            Assert.NotNull(result);
            Assert.Equal(playlistSongGUID, result.PlaylistId);
            Assert.Equal(song.Title, result.Song.Title);
            Assert.Equal(song.Artist, result.Song.Artist);
            Assert.Equal(userGUID, result.AddedByUserId);
            Assert.Equal(addedAt, result.AddedAt);
        }

        [Fact]
        public async Task GetSongByIdAsync_SongExists_ReturnsSong()
        {
            // Arrange
            var playlistRepository = CreateRepositoryWithFakeDb();
            var songGUID = Guid.NewGuid();
            var songTitle = "Song 1";
            var songArtist = "Artist 1";
            var song = new Song { Title = songTitle, Artist = songArtist };
            await playlistRepository.AddSongAsync(song);

            // Act
            var result = await playlistRepository.GetSongByIdAsync(song.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(songTitle, result.Title);
            Assert.Equal(songArtist, result.Artist);
        }

        [Fact]
        public async Task GetSongByIdAsync_SongDoesNotExists_ReturnsNull()
        {
            // Arrange
            var playlistRepository = CreateRepositoryWithFakeDb();

            // Act
            var result = await playlistRepository.GetSongByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdatePlaylistSongAsync_ChangePosition_SucessfullyUpdated()
        {
            // Arrange
            var playlistRepository = CreateRepositoryWithFakeDb();
            var song = new Song { Title = "Song 1", Artist = "Artist 1" };
            var playlistSong = new PlaylistSong
            {
                PlaylistId = Guid.NewGuid(),
                SongId = song.Id,
                Song = song,
                Position = 1,
                AddedByUserId = Guid.NewGuid(),
                AddedAt = DateTime.UtcNow
            };
            await playlistRepository.AddPlaylistSongAsync(playlistSong);

            // Act
            playlistSong.Position = 2;
            await playlistRepository.UpdatePlaylistSongAsync(playlistSong);

            // Assert
            var result = await playlistRepository.GetPlaylistSongBySongIdAsync(song.Id);
            Assert.NotNull(result);
            Assert.Equal(2, result.Position);
        }

        [Fact]
        public async Task RemoveAsync_RemovesPlaylist_SucessfullyRemoves()
        {
            // Arrange
            var playlistRepository = CreateRepositoryWithFakeDb();
            var playlistGUID = Guid.NewGuid();
            var playlist = new Playlist { Id = playlistGUID };
            await playlistRepository.AddAsync(playlist);

            // Act
            await playlistRepository.RemoveAsync(playlist);

            // Assert
            var result = await playlistRepository.GetByIdAsync(playlistGUID);
            Assert.Null(result);
        }
    }
}