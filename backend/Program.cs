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

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173", "http://127.0.0.1:5173", "https://kame-frontend.onrender.com") // frontend origin(s)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Session & Cookie Configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.Cookie.Domain = null;
    }
    else
    {
        // Production (HTTPS, cross-domain on Render)
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});

// Authentication (JWT)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
            Console.WriteLine("Seeding initial data for UTC+03...");

            var barData = new List<(string Name, int StartHour, int EndHour)>
    {
        ("Kame Bar", 8, 22),    // 08:00 to 22:00 Local (05:00 to 19:00 UTC)
        ("Kame Lounge", 18, 4), // 18:00 to 04:00 Local (15:00 to 01:00 UTC)
        ("Kame Garden", 16, 2)  // 16:00 to 02:00 Local (13:00 to 23:00 UTC)
    };

            foreach (var data in barData)
            {
                var bar = new Bar { Name = data.Name };
                bar.SetState(BarState.Closed);

                // Convert Local Target Hour (UTC+3) to UTC
                // We subtract 3 hours to get the correct UTC time
                DateTime localStart = new DateTime(2025, 12, 16, data.StartHour, 0, 0, DateTimeKind.Unspecified);
                DateTime utcStart = localStart.AddHours(-3);

                // Handle the End Time (which might be on the next day)
                DateTime localEnd = new DateTime(2025, 12, 16, data.EndHour, 0, 0, DateTimeKind.Unspecified);
                if (data.EndHour < data.StartHour)
                {
                    localEnd = localEnd.AddDays(1); // Ends the next morning
                }
                DateTime utcEnd = localEnd.AddHours(-3);

                bar.SetSchedule(
                    DateTime.SpecifyKind(utcStart, DateTimeKind.Utc),
                    DateTime.SpecifyKind(utcEnd, DateTimeKind.Utc)
                );

                var playlist = new Playlist();
                await playlistRepo.AddAsync(playlist);
                await barPlaylistEntryRepo.AddEntryAsync(barId: bar.Id, playlistId: playlist.Id);

                bar.CurrentPlaylistId = playlist.Id;
                db.Bars.Add(bar);

                Console.WriteLine($"Added {data.Name} (Local {data.StartHour}:00 is stored as UTC {utcStart.Hour}:00)");
            }

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // UseHttpsRedirection is usually disabled for local HTTP development
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("DevCors");

// Order is critical: Session -> Authentication -> Authorization
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<BarHub>("/hubs/bar");
app.MapControllers();

// Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public partial class Program { }
