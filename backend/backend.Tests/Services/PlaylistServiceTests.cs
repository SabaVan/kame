using backend.Models;
using backend.Services;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using backend.Shared.Enums;
using backend.Utils.Errors;
using backend.Common;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class PlaylistServiceTests
    {
        private readonly Mock<IPlaylistRepository> _playlistRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<ICreditService> _creditServiceMock;
        private readonly PlaylistService _service;

        public PlaylistServiceTests()
        {
            _playlistRepoMock = new Mock<IPlaylistRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _creditServiceMock = new Mock<ICreditService>();

            _service = new PlaylistService(
                _playlistRepoMock.Object,
                _userRepoMock.Object,
                _creditServiceMock.Object
            );
        }

        [Fact]
        public async Task AddSongAsync_ReturnsFailure_WhenPlaylistNotFound()
        {
            var userId = Guid.NewGuid();
            var playlistId = Guid.NewGuid();
            var song = new Song { Id = Guid.NewGuid(), Title = "Song", Artist = "Artist" };

            // Mock user exists
            _userRepoMock.Setup(r => r.GetUserById(userId))
                .Returns(Result<User>.Success(new User { Id = userId }));

            // Playlist does not exist
            _playlistRepoMock.Setup(r => r.GetByIdAsync(playlistId))
                .ReturnsAsync((Playlist?)null);

            var result = await _service.AddSongAsync(userId, playlistId, song);

            Assert.False(result.IsSuccess);
            Assert.Equal("PLAYLIST_NOT_FOUND", result.Error!.Code);
        }

        [Fact]
        public async Task AddSongAsync_ReturnsFailure_WhenSongAlreadyInPlaylist()
        {
            var userId = Guid.NewGuid();
            var playlistId = Guid.NewGuid();
            var song = new Song { Id = Guid.NewGuid(), Title = "Song", Artist = "Artist" };

            var playlistSong = new PlaylistSong
            {
                PlaylistId = playlistId,
                SongId = song.Id,
                Song = song,
                AddedByUserId = userId,
                AddedAt = DateTime.UtcNow
            };

            var playlist = new Playlist
            {
                Id = playlistId,
                Songs = new List<PlaylistSong> { playlistSong }
            };

            _userRepoMock.Setup(r => r.GetUserById(userId))
                .Returns(Result<User>.Success(new User { Id = userId }));
            _playlistRepoMock.Setup(r => r.GetByIdAsync(playlistId))
                .ReturnsAsync(playlist);

            var result = await _service.AddSongAsync(userId, playlistId, song);

            Assert.False(result.IsSuccess);
            Assert.Equal("DUPLICATE_SONG", result.Error!.Code);
        }

        [Fact]
        public async Task AddSongAsync_AddsSong_WhenNotExists()
        {
            var userId = Guid.NewGuid();
            var playlistId = Guid.NewGuid();
            var song = new Song { Id = Guid.NewGuid(), Title = "Song", Artist = "Artist" };

            var playlist = new Playlist { Id = playlistId, Songs = new List<PlaylistSong>() };

            _userRepoMock.Setup(r => r.GetUserById(userId))
                .Returns(Result<User>.Success(new User { Id = userId }));
            _playlistRepoMock.Setup(r => r.GetByIdAsync(playlistId))
                .ReturnsAsync(playlist);
            _playlistRepoMock.Setup(r => r.GetSongByIdAsync(song.Id))
                .ReturnsAsync((Song?)null);

            var result = await _service.AddSongAsync(userId, playlistId, song);

            Assert.True(result.IsSuccess);
            Assert.Equal(song.Id, result.Value!.SongId);
            _playlistRepoMock.Verify(r => r.AddSongAsync(song), Times.Once);
            _playlistRepoMock.Verify(r => r.AddPlaylistSongAsync(It.IsAny<PlaylistSong>()), Times.Once);
        }

        [Fact]
        public async Task GetNextSongAsync_ReturnsFailure_WhenPlaylistNotFound()
        {
            var playlistId = Guid.NewGuid();
            _playlistRepoMock.Setup(r => r.GetByIdAsync(playlistId)).ReturnsAsync((Playlist?)null);

            var result = await _service.GetNextSongAsync(playlistId);

            Assert.False(result.IsSuccess);
            Assert.Equal("PLAYLIST_NOT_FOUND", result.Error!.Code);
        }

        [Fact]
        public async Task GetNextSongAsync_ReturnsFailure_WhenNoSongAvailable()
        {
            var playlistId = Guid.NewGuid();
            var playlist = new Playlist { Id = playlistId, Songs = new List<PlaylistSong>() };
            _playlistRepoMock.Setup(r => r.GetByIdAsync(playlistId)).ReturnsAsync(playlist);

            var result = await _service.GetNextSongAsync(playlistId);

            Assert.False(result.IsSuccess);
            Assert.Equal("NO_SONG_AVAILABLE", result.Error!.Code);
        }

        [Fact]
        public async Task GetNextSongAsync_ReturnsSong_WhenAvailable()
        {
            var playlistId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var song = new Song { Id = Guid.NewGuid(), Title = "Next Song", Artist = "Artist 1" };

            var playlistSong = new PlaylistSong
            {
                PlaylistId = playlistId,
                SongId = song.Id,
                Song = song,
                AddedByUserId = userId,
                AddedAt = DateTime.UtcNow
            };

            var playlist = new Playlist { Id = playlistId, Songs = new List<PlaylistSong> { playlistSong } };

            _playlistRepoMock.Setup(r => r.GetByIdAsync(playlistId)).ReturnsAsync(playlist);

            var result = await _service.GetNextSongAsync(playlistId);

            Assert.True(result.IsSuccess);
            Assert.Equal(song, result.Value);
        }
    }
}