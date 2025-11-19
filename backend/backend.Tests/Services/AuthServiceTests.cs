using System;
using Xunit;
using backend.Services;
using backend.Models;
using backend.Repositories.Interfaces;
using backend.Utils;
using backend.Common;
using backend.Utils.Errors;
using Microsoft.Extensions.Logging.Abstractions;

namespace backend.Tests.Services
{
    public class AuthServiceTest
    {
        // Minimal fake repository implementing only the methods used by AuthService.
        private class FakeUserRepository : IUserRepository
        {
            private readonly Func<string, Result<User>> _getByUsername;
            private readonly Func<User, Result<User>> _saveUser;

            public FakeUserRepository(Func<string, Result<User>> getByUsername, Func<User, Result<User>> saveUser)
            {
                _getByUsername = getByUsername;
                _saveUser = saveUser;
            }

            public Result<User> GetUserByUsername(string username) => _getByUsername(username);

            public Result<User> SaveUser(User user) => _saveUser(user);

            public Result<System.Collections.Generic.List<User>> GetAllUsers() => Result<System.Collections.Generic.List<User>>.Success(new System.Collections.Generic.List<User>());

            public Result<User> UpdateUser(User user) => Result<User>.Failure("UNIMPLEMENTED", "UpdateUser not implemented in fake repo.");

            public Result<User> GetUserById(Guid id) => Result<User>.Failure(StandardErrors.NotFound);

            public Result<bool> UsernameExists(string username) => Result<bool>.Success(false);
        }

        [Fact]
        public void Register_InvalidInput_ReturnsFailure()
        {
            var repo = new FakeUserRepository(
                getByUsername: _ => Result<User>.Failure(StandardErrors.NotFound),
                saveUser: _ => Result<User>.Failure("UNUSED", "unused")
            );
            var svc = new AuthService(repo, NullLogger<AuthService>.Instance);

            var r1 = svc.Register(null!, "password");
            Assert.True(r1.IsFailure);
            Assert.NotNull(r1.Error);

            var r2 = svc.Register("   ", "pwd");
            Assert.True(r2.IsFailure);
            Assert.NotNull(r2.Error);

            var r3 = svc.Register("user", "");
            Assert.True(r3.IsFailure);
            Assert.NotNull(r3.Error);
        }

        [Fact]
        public void Register_UsernameTaken_ReturnsFailure()
        {
            var existing = new User(Guid.NewGuid(), "taken", PasswordHelper.HashPassword("x"), salt: string.Empty);
            var repo = new FakeUserRepository(
                getByUsername: _ => Result<User>.Success(existing),
                saveUser: _ => Result<User>.Failure("SHOULD_NOT", "should not be called")
            );
            var svc = new AuthService(repo, NullLogger<AuthService>.Instance);

            var result = svc.Register("taken", "any");
            Assert.True(result.IsFailure);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void Register_SaveFailure_ReturnsFailure()
        {
            var repo = new FakeUserRepository(
                getByUsername: _ => Result<User>.Failure(StandardErrors.NotFound),
                saveUser: user => Result<User>.Failure("SAVE_FAIL", "Failed to save user")
            );
            var svc = new AuthService(repo, NullLogger<AuthService>.Instance);

            var result = svc.Register("newuser", "pwd");
            Assert.True(result.IsFailure);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void Register_SaveThrows_ReturnsDbError()
        {
            var repo = new FakeUserRepository(
                getByUsername: _ => Result<User>.Failure(StandardErrors.NotFound),
                saveUser: user => throw new InvalidOperationException("simulated DB crash")
            );
            var svc = new AuthService(repo, NullLogger<AuthService>.Instance);

            var result = svc.Register("user", "pwd");
            Assert.True(result.IsFailure);
            Assert.NotNull(result.Error);
            Assert.Contains("Database error", result.Error.Message);
            Assert.Contains("simulated DB crash", result.Error.Message);
        }

        [Fact]
        public void Login_InvalidInput_ReturnsFailure()
        {
            var repo = new FakeUserRepository(
                getByUsername: _ => Result<User>.Failure(StandardErrors.NotFound),
                saveUser: _ => Result<User>.Failure("UNUSED", "unused")
            );
            var svc = new AuthService(repo, NullLogger<AuthService>.Instance);

            var r1 = svc.Login(null!, "pass");
            Assert.True(r1.IsFailure);
            Assert.NotNull(r1.Error);

            var r2 = svc.Login("user", "");
            Assert.True(r2.IsFailure);
            Assert.NotNull(r2.Error);
        }

        [Fact]
        public void Login_UserNotFound_ReturnsFailure()
        {
            var repo = new FakeUserRepository(
                getByUsername: _ => Result<User>.Failure(StandardErrors.NotFound),
                saveUser: _ => Result<User>.Failure("UNUSED", "unused")
            );
            var svc = new AuthService(repo, NullLogger<AuthService>.Instance);

            var result = svc.Login("missing", "pw");
            Assert.True(result.IsFailure);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void Login_WrongPassword_ReturnsUnauthorized()
        {
            var hashed = PasswordHelper.HashPassword("correct");
            var user = new User(Guid.NewGuid(), "bob", hashed, salt: string.Empty);

            var repo = new FakeUserRepository(
                getByUsername: _ => Result<User>.Success(user),
                saveUser: _ => Result<User>.Failure("UNUSED", "unused")
            );
            var svc = new AuthService(repo, NullLogger<AuthService>.Instance);

            var result = svc.Login("bob", "wrong");
            Assert.True(result.IsFailure);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void Login_Success_ReturnsUser()
        {
            var plain = "secret";
            var hashed = PasswordHelper.HashPassword(plain);
            var user = new User(Guid.NewGuid(), "alice", hashed, salt: string.Empty);

            var repo = new FakeUserRepository(
                getByUsername: _ => Result<User>.Success(user),
                saveUser: _ => Result<User>.Failure("UNUSED", "unused")
            );
            var svc = new AuthService(repo, NullLogger<AuthService>.Instance);

            var result = svc.Login("alice", plain);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("alice", result.Value.Username);
        }
    }
}
