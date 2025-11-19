using Xunit;
using Moq;
using backend.Controllers;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using backend.Shared.DTOs;
using backend.Models;
using backend.Hubs;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace backend.Tests.Controllers
{
    // DTO to use instead of anonymous types
    public class ConnectedUserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
    }

    public class BarControllerTests
    {
        private readonly Mock<IBarRepository> _barRepoMock;
        private readonly Mock<IBarService> _barServiceMock;
        private readonly Mock<IBarUserEntryRepository> _barUserRepoMock;
        private readonly Mock<IHubContext<BarHub>> _hubMock;
        private readonly IMapper _mapper;

        public BarControllerTests()
        {
            _barRepoMock = new Mock<IBarRepository>();
            _barServiceMock = new Mock<IBarService>();
            _barUserRepoMock = new Mock<IBarUserEntryRepository>();
            _hubMock = new Mock<IHubContext<BarHub>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Bar, BarDto>();
            });
            _mapper = config.CreateMapper();
        }

        private BarController CreateControllerWithSession(Guid? userId = null)
        {
            var controller = new BarController(
                _barRepoMock.Object,
                _barServiceMock.Object,
                _hubMock.Object,
                _barUserRepoMock.Object,
                _mapper
            );

            var sessionMock = new Mock<ISession>();
            var sessionValues = new Dictionary<string, byte[]>();

            if (userId.HasValue)
                sessionValues["UserId"] = Encoding.UTF8.GetBytes(userId.Value.ToString());

            sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
                .Returns((string key, out byte[]? value) =>
                {
                    if (sessionValues.TryGetValue(key, out var stored))
                    {
                        value = stored;
                        return true;
                    }

                    value = null;
                    return false;
                });

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Session = sessionMock.Object }
            };

            return controller;
        }

        [Fact]
        public async Task GetDefaultBar_ReturnsOk_WhenBarExists()
        {
            var bar = new Bar { Id = Guid.NewGuid(), Name = "DefaultBar" };
            _barServiceMock.Setup(s => s.GetDefaultBar()).ReturnsAsync(bar);

            var controller = CreateControllerWithSession();

            var result = await controller.GetDefaultBar();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedBar = Assert.IsType<BarDto>(okResult.Value);
            Assert.Equal("DefaultBar", returnedBar.Name);
        }

        [Fact]
        public async Task GetDefaultBar_ReturnsNotFound_WhenNoBar()
        {
            _barServiceMock.Setup(s => s.GetDefaultBar()).ReturnsAsync((Bar?)null);

            var controller = CreateControllerWithSession();

            var result = await controller.GetDefaultBar();

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task IsJoined_ReturnsTrue_WhenEntryExists()
        {
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _barUserRepoMock.Setup(r => r.FindEntryAsync(barId, userId))
                .ReturnsAsync(new backend.Common.Result<BarUserEntry>(true, new BarUserEntry(), null));

            var controller = CreateControllerWithSession(userId);

            var result = await controller.IsJoined(barId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True(okResult.Value is bool b && b);
        }

        [Fact]
        public async Task LeaveBar_ReturnsOk_WhenSuccessful()
        {
            var barId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var entryResult = new backend.Common.Result<BarUserEntry>(
                true,
                new BarUserEntry { BarId = barId, UserId = userId },
                null
            );

            _barServiceMock.Setup(s => s.LeaveBar(barId, userId))
                           .Returns(Task.FromResult(entryResult));

            // Mock SignalR
            var mockClients = new Mock<IHubClients>();
            var mockGroup = new Mock<IClientProxy>();
            mockClients.Setup(c => c.Group(barId.ToString())).Returns(mockGroup.Object);
            _hubMock.Setup(h => h.Clients).Returns(mockClients.Object);
            mockGroup.Setup(g => g.SendCoreAsync("BarUsersUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

            var controller = CreateControllerWithSession(userId);

            var result = await controller.LeaveBar(barId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);

            mockGroup.Verify(g => g.SendCoreAsync("BarUsersUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetConnectedUsers_ReturnsUsers()
        {
            // Arrange
            var barId = Guid.NewGuid();
            var users = new List<User>
    {
        new User { Id = Guid.NewGuid(), Username = "Alice" }
    };
            _barUserRepoMock.Setup(r => r.GetUsersInBarAsync(barId)).ReturnsAsync(users);

            var controller = CreateControllerWithSession();

            // Act
            var result = await controller.GetConnectedUsers(barId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult);

            // Now directly cast to List<ConnectedUserDto>
            var returnedUsers = Assert.IsType<List<UserDto>>(okResult.Value!);
            Assert.Single(returnedUsers);
            Assert.Equal("Alice", returnedUsers[0].Username);
        }

    }
}
