using Xunit;
using Moq;
using backend.Models;
using backend.Services;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using backend.Shared.Enums;
using backend.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Tests.Services
{
    public class BarServiceTests
    {
        private readonly Mock<IBarRepository> _barRepoMock;
        private readonly Mock<IBarUserEntryRepository> _barUserRepoMock;
        private readonly Mock<ICreditService> _creditServiceMock;
        private readonly BarService _barService;

        public BarServiceTests()
        {
            _barRepoMock = new Mock<IBarRepository>();
            _barUserRepoMock = new Mock<IBarUserEntryRepository>();
            _creditServiceMock = new Mock<ICreditService>();

            _barService = new BarService(
                _barRepoMock.Object,
                _barUserRepoMock.Object,
                _creditServiceMock.Object
            );
        }

        [Fact]
        public async Task GetDefaultBar_ReturnsFirstBarOrNull()
        {
            var bars = new List<Bar>
            {
                new Bar { Name = "Bar1" },
                new Bar { Name = "Bar2" }
            };
            _barRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(bars);

            var result = await _barService.GetDefaultBar();

            Assert.NotNull(result);
            Assert.Equal("Bar1", result!.Name);
        }

        [Fact]
        public async Task GetDefaultBar_ReturnsNull_WhenNoBars()
        {
            _barRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Bar>());

            var result = await _barService.GetDefaultBar();

            Assert.Null(result);
        }

        [Fact]
        public async Task GetActiveBars_ReturnsBarsThatExist()
        {
            var barIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var bars = barIds.Select(id => new Bar { Id = id }).ToList();

            _barUserRepoMock.Setup(r => r.GetAllUniqueBarIdsAsync()).ReturnsAsync(barIds);
            _barRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                        .ReturnsAsync((Guid id) => bars.FirstOrDefault(b => b.Id == id));

            var result = await _barService.GetActiveBars();

            Assert.Equal(2, result.Count);
            Assert.All(result, b => Assert.Contains(b.Id, barIds));
        }

        [Fact]
        public async Task SetBarState_ChangesState_WhenBarExists()
        {
            var bar = new Bar();
              bar.SetState(BarState.Closed);
            _barRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(bar);
            _barRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _barService.SetBarState(Guid.NewGuid(), BarState.Open);

            Assert.True(result.IsSuccess);
            Assert.Equal(BarState.Open, result.Value!.State);
            _barRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SetBarState_ReturnsFailure_WhenBarNotFound()
        {
            _barRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Bar?)null);

            var result = await _barService.SetBarState(Guid.NewGuid(), BarState.Open);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task EnterBar_Succeeds_WhenBarOpenAndNotDuplicate()
        {
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var bar = new Bar();
            bar.SetState(BarState.Open);
            _barRepoMock.Setup(r => r.GetByIdAsync(barId)).ReturnsAsync(bar);

            var entryResult = Result<BarUserEntry>.Success(new BarUserEntry { BarId = barId, UserId = userId });
            _barUserRepoMock.Setup(r => r.AddEntryAsync(barId, userId)).ReturnsAsync(entryResult);
            _barUserRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _barService.EnterBar(barId, userId);

            Assert.True(result.IsSuccess);
            Assert.Equal(barId, result.Value!.BarId);
            Assert.Equal(userId, result.Value.UserId);
            _barUserRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task EnterBar_Fails_WhenBarClosed()
        {
            var bar = new Bar();
            bar.SetState(BarState.Closed);
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _barRepoMock.Setup(r => r.GetByIdAsync(barId)).ReturnsAsync(bar);

            var result = await _barService.EnterBar(barId, userId);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task LeaveBar_Succeeds_WhenBarOpen()
        {
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var bar = new Bar();
            bar.SetState(BarState.Open);
            _barRepoMock.Setup(r => r.GetByIdAsync(barId)).ReturnsAsync(bar);

            var leaveResult = Result<BarUserEntry>.Success(new BarUserEntry { BarId = barId, UserId = userId });
            _barUserRepoMock.Setup(r => r.RemoveEntryAsync(barId, userId)).ReturnsAsync(leaveResult);
            _barUserRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _barService.LeaveBar(barId, userId);

            Assert.True(result.IsSuccess);
            _barUserRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LeaveBar_Fails_WhenBarClosed()
        {
            var bar = new Bar();
            bar.SetState(BarState.Closed);
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _barRepoMock.Setup(r => r.GetByIdAsync(barId)).ReturnsAsync(bar);

            var result = await _barService.LeaveBar(barId, userId);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task CheckSchedule_OpensAndClosesBars()
        {
            var now = DateTime.UtcNow;

            var bar1 = new Bar();
            var bar2 = new Bar();
            bar1.SetState(BarState.Closed);
            bar2.SetState(BarState.Open);

            var bars = new List<Bar> { bar1, bar2 };
            _barRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(bars);
            _barRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Mock ShouldBeOpen via subclass or wrapper
            bar1.SetSchedule(now.AddHours(-1), now.AddHours(1));
            bar2.SetSchedule(now.AddHours(-2), now.AddHours(-1));

            await _barService.CheckSchedule(now);

            Assert.Equal(BarState.Open, bar1.State);  // should open
            Assert.Equal(BarState.Closed, bar2.State); // should close
            _barRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
