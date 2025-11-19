using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Xunit;
using backend.Controllers;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Common;
using backend.Utils.Errors;

namespace backend.Tests.Controllers
{
    public class ProfileControllerTests
    {
        // Simple in-memory ISession implementation for tests
        private class TestSession : ISession
        {
            private readonly Dictionary<string, byte[]> _store = new();

            public string Id { get; } = Guid.NewGuid().ToString();
            public bool IsAvailable { get; } = true;
            public IEnumerable<string> Keys => _store.Keys;

            public void Clear() => _store.Clear();

            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Remove(string key) => _store.Remove(key);

            public void Set(string key, byte[] value) => _store[key] = value;

            public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
        }

        // Minimal fake repository implementing IUserRepository
        private class FakeUserRepository : IUserRepository
        {
            private readonly Func<Guid, Result<User>> _getById;

            public FakeUserRepository(Func<Guid, Result<User>> getById)
            {
                _getById = getById;
            }

            public Result<List<User>> GetAllUsers() => Result<List<User>>.Success(new List<User>());

            public Result<User> SaveUser(User user) => Result<User>.Failure("UNIMPLEMENTED", "SaveUser not used in tests");

            public Result<User> UpdateUser(User user) => Result<User>.Failure("UNIMPLEMENTED", "UpdateUser not used in tests");

            public Result<User> GetUserByUsername(string username) => Result<User>.Failure(StandardErrors.NotFound);

            public Result<User> GetUserById(Guid id) => _getById(id);

            public Result<bool> UsernameExists(string username) => Result<bool>.Success(false);
        }

        private static IHttpContextAccessor CreateHttpContextAccessorWithSession(ISession session)
        {
            var context = new DefaultHttpContext();
            context.Features.Set<ISessionFeature>(new SessionFeature { Session = session });
            var accessor = new HttpContextAccessor { HttpContext = context };
            return accessor;
        }

        // helper SessionFeature class
        private class SessionFeature : ISessionFeature
        {
            public ISession? Session { get; set; }
        }

        [Fact]
        public void GetProfile_NotLoggedIn_ReturnsUnauthorized()
        {
            var session = new TestSession(); // no UserId set
            var accessor = CreateHttpContextAccessorWithSession(session);
            var repo = new FakeUserRepository(id => Result<User>.Failure(StandardErrors.NotFound));
            var controller = new UsersController(repo, accessor);

            var result = controller.GetProfile();

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public void GetProfile_UserNotFound_ReturnsNotFound()
        {
            var session = new TestSession();
            var id = Guid.NewGuid();
            session.Set("UserId", Encoding.UTF8.GetBytes(id.ToString()));
            var accessor = CreateHttpContextAccessorWithSession(session);

            var repo = new FakeUserRepository(idArg => Result<User>.Failure(StandardErrors.NotFound));
            var controller = new UsersController(repo, accessor);

            var result = controller.GetProfile();

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public void GetProfile_UserExists_ReturnsOkWithUsernameAndCredits()
        {
            var session = new TestSession();
            var id = Guid.NewGuid();
            session.Set("UserId", Encoding.UTF8.GetBytes(id.ToString()));
            var accessor = CreateHttpContextAccessorWithSession(session);

            var user = new User(id, "alice", "hash", salt: string.Empty);
            user.Credits = new Credits(initialAmount: 250);

            var repo = new FakeUserRepository(idArg => Result<User>.Success(user));
            var controller = new UsersController(repo, accessor);

            var result = controller.GetProfile();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            var valueType = ok.Value.GetType();
            var usernameProp = valueType.GetProperty("username", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var creditsProp = valueType.GetProperty("credits", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            Assert.NotNull(usernameProp);
            Assert.NotNull(creditsProp);

            var usernameVal = usernameProp.GetValue(ok.Value)?.ToString();
            var creditsVal = creditsProp.GetValue(ok.Value);

            Assert.Equal("alice", usernameVal);
            Assert.NotNull(creditsVal);
            // creditsVal should equal user.Credits (compare by property values if necessary)
            var creditsType = creditsVal!.GetType();
            var amountProp = creditsType.GetProperty("Amount", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                             ?? creditsType.GetProperty("amount", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (amountProp != null)
            {
                var amount = amountProp.GetValue(creditsVal);
                Assert.Equal(250, Convert.ToInt32(amount));
            }
        }
    }
}