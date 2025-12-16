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
using backend.Shared;

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

#pragma warning disable CS8767
      public bool TryGetValue(string key, out byte[]? value) => _store.TryGetValue(key, out value);
#pragma warning restore CS8767
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
      public ISession Session { get; set; } = null!;
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

    // --- Tests for ClaimDaily endpoint ---

    private class FakeTransactionRepository : ITransactionRepository
    {
      private readonly IEnumerable<CreditTransaction> _logs;

      public FakeTransactionRepository(IEnumerable<CreditTransaction> logs)
      {
        _logs = logs;
      }

      public Task<Result<CreditTransaction>> AddAsync(CreditTransaction creditTransaction) =>
          Task.FromResult(Result<CreditTransaction>.Failure("UNIMPLEMENTED", "Add not used in these tests"));

      public Task<IEnumerable<CreditTransaction>> GetByUserAsync(Guid userId) => Task.FromResult(_logs);

      public Task<IEnumerable<CreditTransaction>> GetByBarAsync(Guid barId) => Task.FromResult(Enumerable.Empty<CreditTransaction>());

      public Task<IEnumerable<CreditTransaction>> GetAllAsync() => Task.FromResult(Enumerable.Empty<CreditTransaction>());
    }

    private class FakeCreditService : ICreditService
    {
      private int _balance;
      private readonly Func<Guid, int, string, backend.Shared.Enums.TransactionType, Guid?, Task<Result<CreditTransaction>>> _onAdd;

      public FakeCreditService(int initialBalance, Func<Guid, int, string, backend.Shared.Enums.TransactionType, Guid?, Task<Result<CreditTransaction>>>? onAdd = null)
      {
        _balance = initialBalance;
        _onAdd = onAdd ?? ((u, a, r, t, b) => Task.FromResult(Result<CreditTransaction>.Success(new CreditTransaction { UserId = u, Amount = a, Reason = r, Type = t })));
      }

      public Task<List<CreditTransaction>> GetLogsForUser(Guid userId) => Task.FromResult(new List<CreditTransaction>());

      public Result<int> GetBalance(Guid userId) => Result<int>.Success(_balance);

      public Task<Result<CreditTransaction>> SpendCredits(Guid userId, int amount, string reason, backend.Shared.Enums.TransactionType type, Guid? barId = null) =>
          Task.FromResult(Result<CreditTransaction>.Failure("UNIMPLEMENTED", "Spend not used"));

      public Task<Result<CreditTransaction>> AddCredits(Guid userId, int amount, string reason, backend.Shared.Enums.TransactionType type, Guid? barId = null)
      {
        _balance += amount;
        return _onAdd(userId, amount, reason, type, barId);
      }
    }

    [Fact]
    public async Task ClaimDaily_BalanceGreaterThan25_ReturnsBadRequest()
    {
      var session = new TestSession();
      var id = Guid.NewGuid();
      session.Set("UserId", Encoding.UTF8.GetBytes(id.ToString()));
      var accessor = CreateHttpContextAccessorWithSession(session);

      var user = new User(id, "bob", "hash", salt: string.Empty);
      var repo = new FakeUserRepository(idArg => Result<User>.Success(user));

      var creditService = new FakeCreditService(initialBalance: 30);
      var txRepo = new FakeTransactionRepository(Enumerable.Empty<CreditTransaction>());

      var controller = new UsersController(repo, accessor, creditService, txRepo);

      var result = await controller.ClaimDaily();

      var bad = Assert.IsType<BadRequestObjectResult>(result);
      Assert.NotNull(bad.Value);
    }

    [Fact]
    public async Task ClaimDaily_BalanceEqual25_ReturnsBadRequest()
    {
      var session = new TestSession();
      var id = Guid.NewGuid();
      session.Set("UserId", Encoding.UTF8.GetBytes(id.ToString()));
      var accessor = CreateHttpContextAccessorWithSession(session);

      var user = new User(id, "eve", "hash", salt: string.Empty);
      var repo = new FakeUserRepository(idArg => Result<User>.Success(user));

      var creditService = new FakeCreditService(initialBalance: 25);
      var txRepo = new FakeTransactionRepository(Enumerable.Empty<CreditTransaction>());

      var controller = new UsersController(repo, accessor, creditService, txRepo);

      var result = await controller.ClaimDaily();

      var bad = Assert.IsType<BadRequestObjectResult>(result);
      Assert.NotNull(bad.Value);
    }

    [Fact]
    public async Task ClaimDaily_Within24Hours_ReturnsBadRequest()
    {
      var session = new TestSession();
      var id = Guid.NewGuid();
      session.Set("UserId", Encoding.UTF8.GetBytes(id.ToString()));
      var accessor = CreateHttpContextAccessorWithSession(session);

      var user = new User(id, "carol", "hash", salt: string.Empty);
      var repo = new FakeUserRepository(idArg => Result<User>.Success(user));

      var recent = DateTime.UtcNow.AddHours(-1);
      var log = new CreditTransaction { UserId = id, Amount = 25, Reason = "daily bonus", CreatedAt = recent, Type = backend.Shared.Enums.TransactionType.Add };
      var txRepo = new FakeTransactionRepository(new[] { log });

      var creditService = new FakeCreditService(initialBalance: 0);
      var controller = new UsersController(repo, accessor, creditService, txRepo);

      var result = await controller.ClaimDaily();

      var bad = Assert.IsType<BadRequestObjectResult>(result);
      Assert.NotNull(bad.Value);
    }

    [Fact]
    public async Task ClaimDaily_After24HoursAndBalanceLow_AddsCredits()
    {
      var session = new TestSession();
      var id = Guid.NewGuid();
      session.Set("UserId", Encoding.UTF8.GetBytes(id.ToString()));
      var accessor = CreateHttpContextAccessorWithSession(session);

      var user = new User(id, "dave", "hash", salt: string.Empty);
      var repo = new FakeUserRepository(idArg => Result<User>.Success(user));

      var old = DateTime.UtcNow.AddDays(-2);
      var log = new CreditTransaction { UserId = id, Amount = 25, Reason = "daily bonus", CreatedAt = old, Type = backend.Shared.Enums.TransactionType.Add };
      var txRepo = new FakeTransactionRepository(new[] { log });

      var creditService = new FakeCreditService(initialBalance: 0);
      var controller = new UsersController(repo, accessor, creditService, txRepo);

      var result = await controller.ClaimDaily();

      var ok = Assert.IsType<OkObjectResult>(result);
      Assert.NotNull(ok.Value);

      // verify new balance returned includes DAILY_AMOUNT (25)
      var valueType = ok.Value.GetType();
      var creditsProp = valueType.GetProperty("credits", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
      Assert.NotNull(creditsProp);
      var creditsVal = creditsProp!.GetValue(ok.Value);
      Assert.Equal(25, Convert.ToInt32(creditsVal));
    }
  }
}