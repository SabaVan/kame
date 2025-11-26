using System;
using System.Threading.Tasks;
using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class BarUserEntryRepositoryTests
    {
        private BarUserEntryRepository CreateRepository(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var db = new AppDbContext(options);
            return new BarUserEntryRepository(db);
        }

        [Fact]
        public async Task TouchEntryAsync_CreatesEntry_WhenMissing()
        {
            var repo = CreateRepository("Touch_Creates");
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act
            await repo.TouchEntryAsync(barId, userId);

            var found = await repo.FindEntryAsync(barId, userId);

            Assert.True(found.IsSuccess);
            var entry = found.Value!;
            Assert.Equal(barId, entry.BarId);
            Assert.Equal(userId, entry.UserId);
            Assert.True((DateTime.UtcNow - entry.EnteredAt) < TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task TouchEntryAsync_UpdatesEnteredAt_WhenExists()
        {
            var repo = CreateRepository("Touch_Updates");
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // seed old entry
            var old = new BarUserEntry(barId, userId) { EnteredAt = DateTime.UtcNow.AddHours(-1) };
            var oldEnteredAt = old.EnteredAt; // capture original timestamp; EF may update the tracked instance later
            await repo.AddEntryAsync(old);
            await repo.SaveChangesAsync();

            // Act
            await Task.Delay(10); // ensure timestamp difference
            await repo.TouchEntryAsync(barId, userId);

            var found = await repo.FindEntryAsync(barId, userId);
            Assert.True(found.IsSuccess);
            var updated = found.Value!;
            Assert.True((DateTime.UtcNow - updated.EnteredAt) < TimeSpan.FromMinutes(1));
            Assert.True(updated.EnteredAt > oldEnteredAt);
        }

        [Fact]
        public async Task GetEntriesOlderThanAsync_ReturnsOnlyOlderEntries()
        {
            var repo = CreateRepository("GetOlder");
            var bar1 = Guid.NewGuid();
            var bar2 = Guid.NewGuid();
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            var e1 = new BarUserEntry(bar1, user1) { EnteredAt = DateTime.UtcNow.AddHours(-2) };
            var e2 = new BarUserEntry(bar2, user2) { EnteredAt = DateTime.UtcNow.AddMinutes(-10) };

            await repo.AddEntryAsync(e1);
            await repo.AddEntryAsync(e2);
            await repo.SaveChangesAsync();

            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            var stale = await repo.GetEntriesOlderThanAsync(cutoff);

            Assert.Single(stale);
            Assert.Equal(e1.BarId, stale[0].BarId);
            Assert.Equal(e1.UserId, stale[0].UserId);
        }
    }
}
