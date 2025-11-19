using backend.Models;
using backend.Services;
using backend.Repositories.Interfaces;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class SongServiceTests
    {
        private readonly Mock<ISongRepository> _repoMock;
        private readonly SongService _service;

        public SongServiceTests()
        {
            _repoMock = new Mock<ISongRepository>();
            _service = new SongService(_repoMock.Object);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchSongsAsync_ReturnsEmpty_WhenQueryIsNullOrWhitespace(string? query)
        {
            var result = await _service.SearchSongsAsync(query);

            Assert.Empty(result);
            _repoMock.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SearchSongsAsync_CallsRepository_WithValidQuery()
        {
            var mockResponse = new List<Song>
            {
                new Song { Title = "Test Song", Artist = "Artist" }
            };

            _repoMock
                .Setup(r => r.SearchAsync("rock", 20))
                .ReturnsAsync(mockResponse);

            var result = await _service.SearchSongsAsync("rock");

            Assert.Equal(mockResponse, result);
            _repoMock.Verify(r => r.SearchAsync("rock", 20), Times.Once);
        }

        [Fact]
        public async Task SearchSongsAsync_PassesLimitCorrectly()
        {
            _repoMock
                .Setup(r => r.SearchAsync("pop", 5))
                .ReturnsAsync(new List<Song>());

            var result = await _service.SearchSongsAsync("pop", limit: 5);

            Assert.Empty(result);
            _repoMock.Verify(r => r.SearchAsync("pop", 5), Times.Once);
        }

        [Fact]
        public async Task SearchSongsAsync_ReturnsRepositoryResults()
        {
            var repoSongs = new List<Song>
            {
                 new Song { Title = "A", Artist = "Artist A" },
                 new Song { Title = "B", Artist = "Artist B" }
            };

            _repoMock
                .Setup(r => r.SearchAsync("test", 20))
                .ReturnsAsync(repoSongs);

            var result = await _service.SearchSongsAsync("test");

            Assert.Same(repoSongs, result);
        }
    }
}