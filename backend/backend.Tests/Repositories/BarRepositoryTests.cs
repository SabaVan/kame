
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using Xunit;

namespace backend.Tests.Repositories
{
    public class BarRepositoryTests
    {
        private BarRepository CreateRepositoryWithFakeDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new AppDbContext(options);

            return new BarRepository(db);
        }

        [Fact]
        public async Task GetAllAsync_NoBars_ReturnsEmptyList()
        {
            // Arrange
            BarRepository repository = CreateRepositoryWithFakeDb();

            // Act
            var bars = await repository.GetAllAsync();

            // Assert
            Assert.Empty(bars);
        }

        [Fact]
        public async Task GetAllAsync_BarsArePresent_ReturnsList()
        {
            // Arrange
            BarRepository repository = CreateRepositoryWithFakeDb();
            var bar1 = new Bar { Id = Guid.NewGuid() };
            var bar2 = new Bar { Id = Guid.NewGuid() };

            await repository.AddAsync(bar1);
            await repository.AddAsync(bar2);
            await repository.SaveChangesAsync();
            // Act
            var bars = await repository.GetAllAsync();

            // Assert
            Assert.NotEmpty(bars);
            Assert.Contains(bars, b => b.Id == bar1.Id);
            Assert.Contains(bars, b => b.Id == bar2.Id);
        }

        [Fact]
        public async Task GetByIdAsync_BarExists_ReturnsBar()
        {
            // Arrange
            BarRepository repository = CreateRepositoryWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            await repository.AddAsync(bar);
            await repository.SaveChangesAsync();
            // Act
            var fromDb = await repository.GetByIdAsync(bar.Id);
            // Assert
            Assert.NotNull(fromDb);
            Assert.Equal(bar.Id, fromDb!.Id);
        }

        [Fact]
        public async Task UpdateAsync_BarExists_UpdatesBar()
        {
            // Arrange
            BarRepository repository = CreateRepositoryWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid(), Name = "Old Name" };
            await repository.AddAsync(bar);
            await repository.SaveChangesAsync();

            // Act
            bar.Name = "New Name";
            var updatedBar = await repository.UpdateAsync(bar);
            await repository.SaveChangesAsync();

            // Assert
            Assert.NotNull(updatedBar);
            Assert.Equal("New Name", updatedBar!.Name);
        }

        [Fact]
        public async Task UpdateAsync_BarDoesNotExist_ReturnsNull()
        {
            // Arrange
            BarRepository repository = CreateRepositoryWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid(), Name = "Old Name" };

            // Act
            var updatedBar = await repository.UpdateAsync(bar);

            // Assert
            Assert.Null(updatedBar);
        }

        [Fact]
        public async Task DeleteAsync_BarExists_DeletesBar()
        {
            // Arrange
            BarRepository repository = CreateRepositoryWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            await repository.AddAsync(bar);
            await repository.SaveChangesAsync();
            // Act
            var result = await repository.DeleteAsync(bar.Id);
            await repository.SaveChangesAsync();
            var fromDb = await repository.GetByIdAsync(bar.Id);
            // Assert
            Assert.True(result);
            Assert.Null(fromDb);
        }

        [Fact]
        public async Task DeleteAsync_BarDoesNotExist_DeletesBar()
        {
            // Arrange
            BarRepository repository = CreateRepositoryWithFakeDb();
            var bar = new Bar { Id = Guid.NewGuid() };
            // Act
            var result = await repository.DeleteAsync(bar.Id);
            // Assert
            Assert.False(result);
        }
    }
}