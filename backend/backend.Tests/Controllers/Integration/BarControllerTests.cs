using backend.Data;
using backend.Models;
using backend.Shared.DTOs;
using backend.Shared.Enums;
using backend.Hubs;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace backend.Tests.Controllers.Integration
{
    public class BarControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;

        public BarControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAllBars_ReturnsOk_WithListOfBars()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Clean state
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();


                var bar1 = new Bar
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Bar One",
                    CurrentPlaylistId = Guid.NewGuid()
                };
                bar1.SetState(BarState.Open);
                bar1.SetSchedule(
                    new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 1, 1, 22, 0, 0, DateTimeKind.Utc)
                );

                var bar2 = new Bar
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Bar Two",
                    CurrentPlaylistId = Guid.NewGuid()
                };
                bar2.SetState(BarState.Closed);
                bar2.SetSchedule(
                    new DateTime(2025, 1, 1, 17, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 1, 2, 03, 0, 0, DateTimeKind.Utc)
                );

                db.Bars.AddRange(bar1, bar2);
                await db.SaveChangesAsync();
            }

            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/bar/all");

            response.EnsureSuccessStatusCode();

            var bars = await response.Content.ReadFromJsonAsync<List<BarDto>>();

            Assert.NotNull(bars);
            Assert.Equal(2, bars.Count);

            // Check automapper and data mapping
            Assert.Contains(bars, b => b.Name == "Test Bar One" && b.State == "Open");
            Assert.Contains(bars, b => b.Name == "Test Bar Two" && b.State == "Closed");
        }

        [Fact]
        public async Task GetDefaultBar_ReturnsOk_WithFirstBar()
        {
            var defaultBarId = Guid.NewGuid();
            var nonDefaultBarId = Guid.NewGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();


                var defaultBar = new Bar
                {
                    Id = defaultBarId,
                    Name = "The First Bar Added",
                    CurrentPlaylistId = Guid.NewGuid()
                };
                defaultBar.SetState(BarState.Open);
                defaultBar.SetSchedule(
                    new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 1, 1, 22, 0, 0, DateTimeKind.Utc)
                );
                db.Bars.Add(defaultBar);

                var secondBar = new Bar
                {
                    Id = nonDefaultBarId,
                    Name = "The Second Bar Added",
                    CurrentPlaylistId = Guid.NewGuid()
                };
                secondBar.SetState(BarState.Closed);
                secondBar.SetSchedule(
                    new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 1, 1, 23, 0, 0, DateTimeKind.Utc)
                );
                db.Bars.Add(secondBar);

                await db.SaveChangesAsync();
            }

            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/bar/default");

            response.EnsureSuccessStatusCode();

            var bar = await response.Content.ReadFromJsonAsync<BarDto>();

            Assert.NotNull(bar);
            // Check that the first bar was returned as a default bar
            Assert.Equal("The First Bar Added", bar.Name);
            Assert.Equal("Open", bar.State);
        }
    }
}