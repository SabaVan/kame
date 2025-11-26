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

var builder = WebApplication.CreateBuilder(args);

// Load .env variables
Env.Load();

// ---------------------------
// Database connection
// ---------------------------
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "kame";

var connectionString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPassword};Database={dbName}";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ---------------------------
// Dependency Injection
// ---------------------------
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

builder.Services.AddHostedService<BarStateUpdaterService>();
// SignalR
builder.Services.AddSignalR();

// ---------------------------
// Controllers, Swagger, CORS, session, authentication
// ---------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: allow frontend origin + credentials
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173", "http://127.0.0.1:5173") // frontend origin(s)
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
    options.Cookie.SameSite = SameSiteMode.None; // allow cookies in cross-origin requests
    // For local development allow insecure cookies so browser will send them over HTTP.
    // In production keep this as Always to require HTTPS.
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

// --- Runtime seeding ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var playlistRepo = scope.ServiceProvider.GetRequiredService<IPlaylistRepository>();
    var barPlaylistEntryRepo = scope.ServiceProvider.GetRequiredService<IBarPlaylistEntryRepository>();

    // Seed Bars if none exist
    if (!db.Bars.Any())
    {
        var bar = new Bar { Name = "Kame Bar" };
        bar.SetState(BarState.Closed);
        bar.SetSchedule(
            new DateTime(2025, 10, 17, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 17, 22, 0, 0, DateTimeKind.Utc)
        );

        // Create playlist and save via repository
        var playlist = new Playlist();
        await playlistRepo.AddAsync(playlist);

        await barPlaylistEntryRepo.AddEntryAsync(barId: bar.Id, playlistId: playlist.Id);

        // Assign playlist to bar
        bar.CurrentPlaylistId = playlist.Id;

        db.Bars.Add(bar);
        db.SaveChanges();
    }
}


// ---------------------------
// Middleware pipeline
// ---------------------------

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


// Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();