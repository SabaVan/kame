using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using backend.Controllers;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using backend.Common;
using backend.Utils.Errors;

namespace backend.Tests.Controllers
{
    public class AuthControllerTests
    {
        // Simple in-memory ISession implementation for tests
        private class TestSession : ISession
        {
            private readonly Dictionary<string, byte[]> _store = new();

            public string Id { get; } = Guid.NewGuid().ToString();
            public bool IsAvailable { get; } = true;
            public IEnumerable<string> Keys => _store.Keys;

            public void Clear() => _store.Clear();

            public Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Remove(string key) => _store.Remove(key);

            public void Set(string key, byte[] value) => _store[key] = value;

#pragma warning disable CS8767
            public bool TryGetValue(string key, out byte[]? value) => _store.TryGetValue(key, out value);
#pragma warning restore CS8767
        }

        // helper SessionFeature class
        private class SessionFeature : ISessionFeature
        {
            public ISession Session { get; set; } = null!;
        }

        private static IHttpContextAccessor CreateHttpContextAccessorWithSession(ISession? session)
        {
            var context = new DefaultHttpContext();
            if (session != null)
                context.Features.Set<ISessionFeature>(new SessionFeature { Session = session });
            var accessor = new HttpContextAccessor { HttpContext = context };
            return accessor;
        }

        [Fact]
        public void Register_Success_SetsSessionAndReturnsUser()
        {
            var user = new User(Guid.NewGuid(), "alice", "hash", salt: string.Empty);

            var mockAuth = new Mock<IAuthService>();
            mockAuth.Setup(s => s.Register(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Result<User>.Success(user));

            var mockUserRepo = new Mock<IUserRepository>();
            var mockBarRepo = new Mock<IBarUserEntryRepository>();

            var session = new TestSession();
            var accessor = CreateHttpContextAccessorWithSession(session);

            var controller = new AuthController(mockAuth.Object, mockUserRepo.Object,
                NullLogger<AuthController>.Instance, mockBarRepo.Object, accessor);

            var req = new RegisterRequest { Username = "alice", Password = "pw" };
            var result = controller.Register(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(user.Username, ((User)ok.Value!).Username);

            var stored = session.TryGetValue("UserId", out var bytes);
            Assert.True(stored);
            Assert.Equal(user.Id.ToString(), Encoding.UTF8.GetString(bytes!));
        }

        [Fact]
        public void Register_Failure_ReturnsBadRequest()
        {
            var mockAuth = new Mock<IAuthService>();
            mockAuth.Setup(s => s.Register(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Result<User>.Failure("ERR", "failed"));

            var controller = new AuthController(mockAuth.Object, Mock.Of<IUserRepository>(),
                NullLogger<AuthController>.Instance, Mock.Of<IBarUserEntryRepository>(), CreateHttpContextAccessorWithSession(new TestSession()));

            var req = new RegisterRequest { Username = "u", Password = "p" };
            var result = controller.Register(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Login_Success_SetsSessionAndReturnsUser()
        {
            var user = new User(Guid.NewGuid(), "bob", "hash", salt: string.Empty);

            var mockAuth = new Mock<IAuthService>();
            mockAuth.Setup(s => s.Login(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Result<User>.Success(user));

            var session = new TestSession();
            var accessor = CreateHttpContextAccessorWithSession(session);

            var controller = new AuthController(mockAuth.Object, Mock.Of<IUserRepository>(),
                NullLogger<AuthController>.Instance, Mock.Of<IBarUserEntryRepository>(), accessor);

            var req = new LoginRequest { Username = "bob", Password = "pw" };
            var result = controller.Login(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(user.Username, ((User)ok.Value!).Username);

            var stored = session.TryGetValue("UserId", out var bytes);
            Assert.True(stored);
            Assert.Equal(user.Id.ToString(), Encoding.UTF8.GetString(bytes!));
        }

        [Fact]
        public void Login_Failure_ReturnsBadRequest()
        {
            var mockAuth = new Mock<IAuthService>();
            mockAuth.Setup(s => s.Login(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Result<User>.Failure("ERR", "unauthorized"));

            var controller = new AuthController(mockAuth.Object, Mock.Of<IUserRepository>(),
                NullLogger<AuthController>.Instance, Mock.Of<IBarUserEntryRepository>(), CreateHttpContextAccessorWithSession(new TestSession()));

            var req = new LoginRequest { Username = "x", Password = "y" };
            var result = controller.Login(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Logout_NoHttpContext_ReturnsBadRequest()
        {
            var controller = new AuthController(Mock.Of<IAuthService>(), Mock.Of<IUserRepository>(),
                NullLogger<AuthController>.Instance, Mock.Of<IBarUserEntryRepository>(), new HttpContextAccessor { HttpContext = null });

            var result = await controller.Logout();
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Logout_NoSession_ReturnsBadRequest()
        {
            var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() }; // no session feature
            var controller = new AuthController(Mock.Of<IAuthService>(), Mock.Of<IUserRepository>(),
                NullLogger<AuthController>.Instance, Mock.Of<IBarUserEntryRepository>(), accessor);

            var result = await controller.Logout();
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Logout_WithUser_ClearsSessionAndCallsBarRepo()
        {
            var userId = Guid.NewGuid();

            var mockBarRepo = new Mock<IBarUserEntryRepository>();
            // return empty list of bars (successful path still calls SaveChangesAsync)
            mockBarRepo.Setup(b => b.GetBarsForUserAsync(userId)).ReturnsAsync(new List<backend.Models.Bar>());
            // RemoveEntryAsync returns a Result<BarUserEntry>; return a completed Task with a Failure/Success Result
            mockBarRepo.Setup(b => b.RemoveEntryAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                       .ReturnsAsync(Result<backend.Models.BarUserEntry>.Failure("UNUSED", "not used in this test"));
             mockBarRepo.Setup(b => b.SaveChangesAsync()).Returns(Task.CompletedTask).Verifiable();

            var session = new TestSession();
            session.SetString("UserId", userId.ToString());
            var accessor = CreateHttpContextAccessorWithSession(session);

            var controller = new AuthController(Mock.Of<IAuthService>(), Mock.Of<IUserRepository>(),
                NullLogger<AuthController>.Instance, mockBarRepo.Object, accessor);

            var result = await controller.Logout();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            // session should be cleared
            Assert.False(session.TryGetValue("UserId", out _));

            mockBarRepo.Verify(b => b.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public void GetCurrentUserId_NoHttpContext_ReturnsBadRequest()
        {
            var controller = new AuthController(Mock.Of<IAuthService>(), Mock.Of<IUserRepository>(),
                NullLogger<AuthController>.Instance, Mock.Of<IBarUserEntryRepository>(), new HttpContextAccessor { HttpContext = null });

            var result = controller.GetCurrentUserId();
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void GetCurrentUserId_NoUserId_ReturnsUnauthorized()
        {
            var accessor = CreateHttpContextAccessorWithSession(new TestSession()); // session but no UserId
            var controller = new AuthController(Mock.Of<IAuthService>(), Mock.Of<IUserRepository>(),
                NullLogger<AuthController>.Instance, Mock.Of<IBarUserEntryRepository>(), accessor);

            var result = controller.GetCurrentUserId();
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void GetCurrentUserId_WithUserId_ReturnsOk()
        {
            var userId = Guid.NewGuid().ToString();
            var session = new TestSession();
            session.SetString("UserId", userId);
            var accessor = CreateHttpContextAccessorWithSession(session);

            var controller = new AuthController(Mock.Of<IAuthService>(), Mock.Of<IUserRepository>(),
                NullLogger<AuthController>.Instance, Mock.Of<IBarUserEntryRepository>(), accessor);

            var result = controller.GetCurrentUserId();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            // Value is anonymous type with userId property; use reflection
            var valueType = ok.Value.GetType();
            var prop = valueType.GetProperty("userId") ?? valueType.GetProperty("UserId");
            var val = prop?.GetValue(ok.Value)?.ToString();
            Assert.Equal(userId, val);
        }
    }
}