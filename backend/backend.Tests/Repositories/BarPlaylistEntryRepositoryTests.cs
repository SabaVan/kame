
using backend.Models;
using Xunit;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using Azure.Core;

namespace backend.Tests.Repositories
{
    public class BarPlaylistEntryRepositoryTests
    {
        private BarPlaylistEntryRepository CreateRepositoryWithFakeDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new AppDbContext(options);

            return new BarPlaylistEntryRepository(db);
        }

        private (BarPlaylistEntryRepository repo, AppDbContext context) CreateRepositoryAndContextWithFakeDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            AppDbContext db = new AppDbContext(options);
            var repo = new BarPlaylistEntryRepository(db);
            return (repo, db);
        }

        private async Task addBarToContext(AppDbContext context, Bar bar)
        {
            context.Bars.Add(bar);
            await context.SaveChangesAsync();
        }

        private async Task addPlaylistToContext(AppDbContext context, Playlist playlist)
        {
            context.Playlists.Add(playlist);
            await context.SaveChangesAsync();
        }


        [Fact]
        public async Task AddEntryAsync_barIdAndPlaylistId_AddsEntry_ReturnSuccessfulResult()
        {
            // Arrange
            BarPlaylistEntryRepository repository = CreateRepositoryWithFakeDb();
            var barId = Guid.NewGuid();
            var playlistId = Guid.NewGuid();

            var entry = new BarPlaylistEntry(barId, playlistId);

            // Act
            await repository.AddEntryAsync(entry);

            // Assert
            var fromDb = await repository.FindEntryAsync(barId, playlistId);
            Assert.NotNull(fromDb.Value);
            Assert.Equal(barId, fromDb.Value.BarId!);
            Assert.Equal(playlistId, fromDb.Value.PlaylistId);
        }

        [Fact]
        public async Task AddEntryAsync_BarPlaylistEntry_AddsEntryWhenItExists_ReturnFailureResult()
        {
            // Arrange
            BarPlaylistEntryRepository repository = CreateRepositoryWithFakeDb();
            var barId = Guid.NewGuid();
            var playlistId = Guid.NewGuid();

            var entry = new BarPlaylistEntry(barId, playlistId);
            await repository.AddEntryAsync(entry);

            // Act
            var result = await repository.AddEntryAsync(entry); // Try to add again

            // Assert
            Assert.Null(result.Value);
            Assert.True(result.IsFailure);
        }


        [Fact]
        public async Task AddEntryAsync_BarAndPlaylist_AddsEntryWhenItExists_ReturnsSuccessful()
        {
            // Arrange
            BarPlaylistEntryRepository repository = CreateRepositoryWithFakeDb();
            var bar = new Bar();
            var playlist = new Playlist();

            // Act
            var result = await repository.AddEntryAsync(bar, playlist);

            // Assert
            var fromDb = await repository.FindEntryAsync(bar, playlist);
            Assert.NotNull(fromDb.Value);
            Assert.Equal(bar.Id, fromDb.Value.BarId!);
            Assert.Equal(playlist.Id, fromDb.Value.PlaylistId);
        }


        [Fact]
        public async Task RemoveEntryAsync_BarIdAndPlaylistId_RemovesPlaylist_SuccessfullyRemoves()
        {
            // Arrange
            BarPlaylistEntryRepository repository = CreateRepositoryWithFakeDb();
            var barId = Guid.NewGuid();
            var playlistId = Guid.NewGuid();
            await repository.AddEntryAsync(barId, playlistId);

            // Act
            await repository.RemoveEntryAsync(barId, playlistId);

            // Assert
            var result = await repository.FindEntryAsync(barId, playlistId);
            Assert.NotNull(result.Value);
            Assert.True(result.IsSuccess);
            Assert.Equal(result.Value.BarId, barId);
            Assert.Equal(result.Value.PlaylistId, playlistId);
        }

        [Fact]
        public async Task RemoveEntryAsync_BarIdAndPlaylistId_RemovesNonExistingEntry_ReturnsFailure()
        {
            // Arrange
            BarPlaylistEntryRepository repository = CreateRepositoryWithFakeDb();
            var barId = Guid.NewGuid();
            var playlistId = Guid.NewGuid();

            // Act
            await repository.RemoveEntryAsync(barId, playlistId);

            // Assert
            var result = await repository.FindEntryAsync(barId, playlistId);
            Assert.Null(result.Value);
            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task RemoveEntryAsync_BarAndPlaylist_RemovesNonExistingEntry_ReturnsFailure()
        {
            // Arrange
            BarPlaylistEntryRepository repository = CreateRepositoryWithFakeDb();
            var bar = new Bar();
            var playlist = new Playlist();

            // Act
            await repository.RemoveEntryAsync(bar, playlist);

            // Assert
            var result = await repository.FindEntryAsync(bar, playlist);
            Assert.Null(result.Value);
            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task RemoveEntryAsync_Entry_RemovesNonExistingEntry_ReturnsFailure()
        {
            // Arrange
            BarPlaylistEntryRepository repository = CreateRepositoryWithFakeDb();
            var bar = new Bar();
            var playlist = new Playlist();
            var entry = new BarPlaylistEntry(bar, playlist);

            // Act
            await repository.RemoveEntryAsync(entry);

            // Assert
            var result = await repository.FindEntryAsync(entry);
            Assert.Null(result.Value);
            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task GetPlaylistsForBarAsync_BarId_ReturnsPlaylists()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var playlist1 = new Playlist { Id = Guid.NewGuid() };
            var playlist2 = new Playlist { Id = Guid.NewGuid() };

            await addBarToContext(context, bar);
            await addPlaylistToContext(context, playlist1);
            await addPlaylistToContext(context, playlist2);

            await repository.AddEntryAsync(bar.Id, playlist1.Id);
            await repository.AddEntryAsync(bar.Id, playlist2.Id);
            // Act
            var playlists = await repository.GetPlaylistsForBarAsync(bar.Id);
            // Assert
            Assert.Contains(playlists, p => p.Id == playlist1.Id);
            Assert.Contains(playlists, p => p.Id == playlist2.Id);
        }

        [Fact]
        public async Task GetBarsForPlaylistAsync_PlaylistId_ReturnsBars()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var playlist = new Playlist { Id = Guid.NewGuid() };
            var bar1 = new Bar { Id = Guid.NewGuid() };
            var bar2 = new Bar { Id = Guid.NewGuid() };

            await addBarToContext(context, bar1);
            await addBarToContext(context, bar2);
            await addPlaylistToContext(context, playlist);

            await repository.AddEntryAsync(bar1.Id, playlist.Id);
            await repository.AddEntryAsync(bar2.Id, playlist.Id);
            // Act
            var bars = await repository.GetBarsForPlaylistAsync(playlist.Id);
            // Assert
            Assert.Contains(bars, b => b.Id == bar1.Id);
            Assert.Contains(bars, b => b.Id == bar2.Id);
        }
    }
}