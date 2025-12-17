using backend.Models;
using Xunit;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using backend.Data;


namespace backend.Tests.Repositories
{
    public class BarUserEntryRepositoryTests
    {
        private BarUserEntryRepository CreateRepositoryWithFakeDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            AppDbContext db = new AppDbContext(options);

            return new BarUserEntryRepository(db);
        }

        private (BarUserEntryRepository repo, AppDbContext context) CreateRepositoryAndContextWithFakeDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            AppDbContext db = new AppDbContext(options);
            var repo = new BarUserEntryRepository(db);
            return (repo, db);
        }

        private async Task addBarAndUserToContext(AppDbContext context, Guid barId, Guid userId)
        {
            context.Bars.Add(new Bar { Id = barId });
            context.Users.Add(new User { Id = userId });
            await context.SaveChangesAsync();
        }

        private async Task addBarAndUserToContext(AppDbContext context, Bar bar, User user)
        {
            context.Bars.Add(bar);
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        private async Task addBarToContext(AppDbContext context, Bar bar)
        {
            context.Bars.Add(bar);
            await context.SaveChangesAsync();
        }

        private async Task addUserToContext(AppDbContext context, User user)
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task AddEntryAsync_AddsNewEntry_ReturnsSuccessResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            await addBarAndUserToContext(context, barId, userId);
            // Act
            var result = await repository.AddEntryAsync(barId, userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(barId, result.Value.BarId);
            Assert.Equal(userId, result.Value.UserId);
        }

        [Fact]
        public async Task AddEntryAsync_barIdAndUserId_AddsExistingEntry_ReturnsFailureResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();


            await addBarAndUserToContext(context, barId, userId);

            await repository.AddEntryAsync(barId, userId);

            // Act
            var result = await repository.AddEntryAsync(barId, userId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task AddEntryAsync_barIdAndUserId_TheUserIsNotInDb_ReturnsFailureResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var fakeUserId = Guid.NewGuid();

            await addBarToContext(context, bar);
            await repository.AddEntryAsync(bar.Id, fakeUserId);
            // Act
            var result = await repository.AddEntryAsync(bar.Id, fakeUserId);
            // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task AddEntryAsync_WithBarAndUser_AddsNewEntry_ReturnsSuccessResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var user = new User { Id = Guid.NewGuid() };

            await addBarAndUserToContext(context, bar, user);
            // Act
            var result = await repository.AddEntryAsync(bar, user);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(bar.Id, result.Value.BarId);
            Assert.Equal(user.Id, result.Value.UserId);
        }

        [Fact]
        public async Task AddEntryAsync_WithBarAndUser_AddsExistingEntry_ReturnsFailureResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var user = new User { Id = Guid.NewGuid() };

            await addBarAndUserToContext(context, bar, user);
            await repository.AddEntryAsync(bar, user); // add first time
                                                       // Act
            var result = await repository.AddEntryAsync(bar, user); // try to add again
                                                                    // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task AddEntryAsync_WithBarUserEntry_AddsNewEntry_ReturnsSuccessResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            await addBarAndUserToContext(context, barId, userId);

            var entry = new BarUserEntry(barId, userId);
            // Act
            var result = await repository.AddEntryAsync(entry);
            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(barId, result.Value.BarId);
            Assert.Equal(userId, result.Value.UserId);
        }

        [Fact]
        public async Task AddEntryAsync_WithBarUserEntry_AddsExistingEntry_ReturnsFailureResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var entry = new BarUserEntry(barId, userId);

            await addBarAndUserToContext(context, barId, userId);

            await repository.AddEntryAsync(entry);
            // Act
            var result = await repository.AddEntryAsync(entry);
            // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task RemoveEntryAsync_barIdAndUserId_RemovesEntry_ReturnsSuccessResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            await addBarAndUserToContext(context, barId, userId);

            await repository.AddEntryAsync(barId, userId);
            // Act
            var result = await repository.RemoveEntryAsync(barId, userId);
            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(barId, result.Value.BarId);
            Assert.Equal(userId, result.Value.UserId);
        }

        [Fact]
        public async Task RemoveEntryAsync_barIdAndUserId_RemovesUnexistantEntry_ReturnsFailureResult()
        {
            // Arrange
            var repository = CreateRepositoryWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            await repository.AddEntryAsync(barId, userId);

            var unexistantBarId = Guid.NewGuid();
            var unexistantUserId = Guid.NewGuid();
            // Act
            var result = await repository.RemoveEntryAsync(unexistantBarId, unexistantUserId);
            // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task RemoveEntryAsync_WithBarAndUser_RemovesEntry_ReturnsSuccessResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var user = new User { Id = Guid.NewGuid() };

            await addBarAndUserToContext(context, bar, user);
            await repository.AddEntryAsync(bar, user);
            // Act
            var result = await repository.RemoveEntryAsync(bar, user);
            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(bar.Id, result.Value.BarId);
            Assert.Equal(user.Id, result.Value.UserId);
        }

        [Fact]
        public async Task RemoveEntryAsync_WithBarAndUser_RemovesUnexistantEntry_ReturnsFailureResult()
        {
            // Arrange
            var repository = CreateRepositoryWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var user = new User { Id = Guid.NewGuid() };
            // Act
            var result = await repository.RemoveEntryAsync(bar, user);
            // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task RemoveEntryAsync_WithBarUserEntry_RemovesEntry_ReturnsSuccessResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var entry = new BarUserEntry(barId, userId);

            await addBarAndUserToContext(context, barId, userId);
            await repository.AddEntryAsync(entry);
            // Act
            var result = await repository.RemoveEntryAsync(entry);
            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(barId, result.Value.BarId);
            Assert.Equal(userId, result.Value.UserId);
        }

        [Fact]
        public async Task RemoveEntryAsync_BarUserEntry_RemovesNonexistantEntry_ReturnsFailureResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var entry = new BarUserEntry(barId, userId);
            await addBarAndUserToContext(context, barId, userId);
            // Act
            var result = await repository.RemoveEntryAsync(entry);
            // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task GetBarsForUserAsync_ReturnsCorrectBars()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar1 = new Bar { Id = Guid.NewGuid() };
            var bar2 = new Bar { Id = Guid.NewGuid() };
            var user = new User { Id = Guid.NewGuid() };

            await addUserToContext(context, user);
            await addBarToContext(context, bar1);
            await addBarToContext(context, bar2);

            await repository.AddEntryAsync(bar1, user);
            await repository.AddEntryAsync(bar2, user);
            // Act
            var bars = await repository.GetBarsForUserAsync(user.Id);
            // Assert
            Assert.Equal(2, bars.Count);
            Assert.Contains(bars, b => b.Id == bar1.Id);
            Assert.Contains(bars, b => b.Id == bar2.Id);
        }

        [Fact]
        public async Task GetUsersInBarAsync_ThereAreUsersInBar_ReturnsCorrectUsers()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var user1 = new User { Id = Guid.NewGuid() };
            var user2 = new User { Id = Guid.NewGuid() };

            await addBarToContext(context, bar);
            await addUserToContext(context, user1);
            await addUserToContext(context, user2);

            await repository.AddEntryAsync(bar, user1);
            await repository.AddEntryAsync(bar, user2);
            // Act
            var users = await repository.GetUsersInBarAsync(bar.Id);
            // Assert
            Assert.Equal(2, users.Count);
            Assert.Contains(users, u => u.Id == user1.Id);
            Assert.Contains(users, u => u.Id == user2.Id);
        }


        [Fact]
        public async Task GetUsersInBarAsync_UsersDoNotExistInBar_ReturnsEmptyList()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var user1 = new User { Id = Guid.NewGuid() };
            var user2 = new User { Id = Guid.NewGuid() };

            await addBarToContext(context, bar);
            await addUserToContext(context, user1);
            await addUserToContext(context, user2);
            // Act
            var users = await repository.GetUsersInBarAsync(bar.Id);
            // Assert
            Assert.Empty(users);
            Assert.DoesNotContain(users, u => u.Id == user2.Id);
        }

        [Fact]
        public async Task FindEntryAsync_WithBarAndUser_FindsExistingEntry_ReturnsSuccessResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var user = new User { Id = Guid.NewGuid() };
            await addBarAndUserToContext(context, bar, user);
            await repository.AddEntryAsync(bar, user);
            // Act
            var result = await repository.FindEntryAsync(bar, user);
            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(bar.Id, result.Value.BarId);
            Assert.Equal(user.Id, result.Value.UserId);
        }

        [Fact]
        public async Task FindEntryAsync_WithBarAndUser_ThereIsnoAssociation_ReturnsFailureResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            var user = new User { Id = Guid.NewGuid() };
            await addBarAndUserToContext(context, bar, user);
            // Act
            var result = await repository.FindEntryAsync(bar, user);
            // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task FindEntryAsync_WithBarUserEntry_FindsExistingEntry_ReturnsSuccessResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var entry = new BarUserEntry(barId, userId);
            await addBarAndUserToContext(context, barId, userId);
            await repository.AddEntryAsync(entry);
            // Act
            var result = await repository.FindEntryAsync(entry);
            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(barId, result.Value.BarId);
            Assert.Equal(userId, result.Value.UserId);
        }


        [Fact]
        public async Task FindEntryAsync_WithBarUserEntry_ThereIsnoAssociation_ReturnsFailureResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var entry = new BarUserEntry(barId, userId);
            await addBarAndUserToContext(context, barId, userId);
            // Act
            var result = await repository.FindEntryAsync(entry);
            // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task FindEntryAsync_barIdAndUserId_ThereIsAssociation_ReturnsSuccessResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            await addBarAndUserToContext(context, barId, userId);
            await repository.AddEntryAsync(barId, userId);
            // Act
            var result = await repository.FindEntryAsync(barId, userId);
            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(barId, result.Value.BarId);
            Assert.Equal(userId, result.Value.UserId);
        }

        [Fact]
        public async Task FindEntryAsync_barIdAndUserId_ThereIsNoAssociation_ReturnsFailureResult()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            await addBarAndUserToContext(context, barId, userId);
            // Act
            var result = await repository.FindEntryAsync(barId, userId);
            // Assert
            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task SaveChangesAsync_CommitsChangesToDatabase()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            await addBarAndUserToContext(context, barId, userId);

            var entry = new BarUserEntry(barId, userId);
            await context.BarUserEntries.AddAsync(entry);

            // Act
            await repository.SaveChangesAsync();

            // Assert
            var savedEntry = await context.BarUserEntries.FindAsync(barId, userId);
            Assert.NotNull(savedEntry);
            Assert.Equal(barId, savedEntry.BarId);
            Assert.Equal(userId, savedEntry.UserId);
        }

        [Fact]
        public async Task GetAllUniqueBarIdsAsync_ReturnsDistinctBarIds()
        {
            // Arrange
            var (repository, context) = CreateRepositoryAndContextWithFakeDb();
            var bar1 = new Bar { Id = Guid.NewGuid() };
            var bar2 = new Bar { Id = Guid.NewGuid() };
            var user1 = new User { Id = Guid.NewGuid() };
            var user2 = new User { Id = Guid.NewGuid() };

            await addBarToContext(context, bar1);
            await addBarToContext(context, bar2);
            await addUserToContext(context, user1);
            await addUserToContext(context, user2);

            await repository.AddEntryAsync(bar1.Id, user1.Id);
            await repository.AddEntryAsync(bar1.Id, user2.Id);
            await repository.AddEntryAsync(bar2.Id, user1.Id);
            // Act
            var uniqueBarIds = await repository.GetAllUniqueBarIdsAsync();
            // Assert
            Assert.Equal(2, uniqueBarIds.Count);
            Assert.Contains(bar1.Id, uniqueBarIds);
            Assert.Contains(bar2.Id, uniqueBarIds);
        }
    }
}