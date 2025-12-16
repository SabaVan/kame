using backend.Data;
using backend.Hubs;
using backend.Services;
using backend.Services.Interfaces;
using backend.Repositories;
using backend.Repositories.Interfaces;
using backend.Shared.Enums;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;
using backend.Services.Background;

if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true)
{
  // Disable file watching in production (Render)
  var disableFileWatching = Environment.GetEnvironmentVariable("DISABLE_FILE_WATCHING") ?? "true";
  if (disableFileWatching.Equals("true", StringComparison.OrdinalIgnoreCase))
  {
    AppContext.SetSwitch("Microsoft.AspNetCore.DisableInotifyFileWatcher", true);
  }
}

var builder = WebApplication.CreateBuilder(args);

// Load .env variables
Env.Load();

// Database connection
if (!builder.Environment.IsEnvironment("Testing"))
{
  var renderDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
  string connectionString;

  if (!string.IsNullOrEmpty(renderDbUrl) && renderDbUrl.StartsWith("postgresql://"))
  {
    // Parse Render's PostgreSQL connection string
    try
    {
      var uri = new Uri(renderDbUrl);
      var db = uri.AbsolutePath.Trim('/');
      var user = uri.UserInfo.Split(':')[0];
      var password = uri.UserInfo.Split(':')[1];
      var port = uri.Port > 0 ? uri.Port : 5432;

      connectionString = $"Host={uri.Host};Port={port};Database={db};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";

      Console.WriteLine($"Using Render database: {uri.Host}:{port}/{db}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error parsing DATABASE_URL, falling back to local: {ex.Message}");
      // Fall through to local development
      connectionString = BuildLocalConnectionString();
    }
  }
  else
  {
    // Local development
    connectionString = BuildLocalConnectionString();
  }

  builder.Services.AddDbContext<AppDbContext>(options =>
      options.UseNpgsql(connectionString));
}

// Helper method
string BuildLocalConnectionString()
{
  Env.Load();
  var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
  var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
  var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
  var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
  var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "kame";

  return $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPassword};Database={dbName}";
}

// Dependency Injection
builder.Services.AddScoped<IBarService, BarService>();
builder.Services.AddScoped<IPlaylistService, PlaylistService>();
builder.Services.AddScoped<ISongService, SongService>();

builder.Services.AddScoped<IBarRepository, BarRepository>();
builder.Services.AddScoped<IBarUserEntryRepository, BarUserEntryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPlaylistRepository, PlaylistRepository>();
builder.Services.AddScoped<ISongRepository, ExternalAPISongRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IBarPlaylistEntryRepository, BarPlaylistEntryRepository>();
//builder.Services.AddScoped<IBidRepository, BidRepository>();
//builder.Services.AddScoped<ICreditManager, CreditManager>();

builder.Services.AddHttpClient<ISongRepository, ExternalAPISongRepository>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddAutoMapper(typeof(Program));

if (!builder.Environment.IsEnvironment("Testing"))
{
  builder.Services.AddHostedService<BarStateUpdaterService>();
  builder.Services.AddHostedService<BarUserCleanupService>();
}
// SignalR
builder.Services.AddSignalR();

// Controllers, Swagger, CORS, session, authentication
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: allow frontend origin + credentials
builder.Services.AddCors(options =>
{
  options.AddPolicy("DevCors", policy =>
      policy.WithOrigins("http://localhost:5173", "https://localhost:5173", "http://127.0.0.1:5173", "https://kame-frontend.onrender.com") // frontend origin(s)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
  );
});


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
  options.IdleTimeout = TimeSpan.FromMinutes(30);
  options.Cookie.HttpOnly = true;
  options.Cookie.IsEssential = true;
  options.Cookie.SameSite = SameSiteMode.None;
  options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
});

// JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
              System.Text.Encoding.UTF8.GetBytes("super-secret-local-key"))
      };
    });

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
  using (var scope = app.Services.CreateScope())
  {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
      Console.WriteLine("Checking for pending migrations...");
      var pendingMigrations = db.Database.GetPendingMigrations().ToList();
      if (pendingMigrations.Any())
      {
        Console.WriteLine($"Applying {pendingMigrations.Count} migration(s): {string.Join(", ", pendingMigrations)}");
        db.Database.Migrate();
        Console.WriteLine("Migrations applied successfully.");
      }
      else
      {
        Console.WriteLine("No pending migrations.");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Migration error: {ex.Message}");
      // Continue - maybe tables already exist or we're in a state where migrations can't run
    }

    var playlistRepo = scope.ServiceProvider.GetRequiredService<IPlaylistRepository>();
    var barPlaylistEntryRepo = scope.ServiceProvider.GetRequiredService<IBarPlaylistEntryRepository>();

    // Seed Bars if none exist
    if (!db.Bars.Any())
    {
      Console.WriteLine("Seeding initial data...");

      var bar = new Bar { Name = "Kame Bar" };
      bar.SetState(BarState.Closed);
      bar.SetSchedule(
          new DateTime(2025, 10, 17, 8, 0, 0, DateTimeKind.Utc),
          new DateTime(2025, 10, 17, 22, 0, 0, DateTimeKind.Utc)
      );

      var playlist = new Playlist();
      await playlistRepo.AddAsync(playlist);

      await barPlaylistEntryRepo.AddEntryAsync(barId: bar.Id, playlistId: playlist.Id);

      bar.CurrentPlaylistId = playlist.Id;

      db.Bars.Add(bar);
      await db.SaveChangesAsync();

      Console.WriteLine("Initial seeding completed.");
    }
    else
    {
      Console.WriteLine("Database already contains data, skipping seeding.");
    }
  }
}

// Middleware pipeline

// Enable CORS first
app.UseCors("DevCors");

// Optional: redirect HTTP → HTTPS
app.UseHttpsRedirection();

// Session before controllers and hubs
app.UseSession();

// Authentication + Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hubs
app.MapHub<BarHub>("/hubs/bar");

// Map controllers
app.MapControllers();

// Swagger in development
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.Run();

public partial class Program { }