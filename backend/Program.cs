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

builder.Services.AddHttpClient<ISongRepository, ExternalAPISongRepository>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddHostedService<BarStateUpdaterService>();
builder.Services.AddHostedService<backend.Services.Background.BarUserCleanupService>();

builder.Services.AddSignalR();

// ---------------------------
// Controllers & Swagger
// ---------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------------------
// CORS Policy
// ---------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); 
});

// ---------------------------
// Session & Cookie Configuration
// ---------------------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    
    // Lax allows cross-port (5173 to 5000) requests on localhost without HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax; 
    
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
});

// ---------------------------
// Authentication (JWT)
// ---------------------------
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

// ---------------------------
// Database Migration & Seeding
// ---------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Requirement: Automatic Migrations
    try 
    {
        if (db.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("Applying pending migrations...");
            db.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration.");
    }

    // Seeding Logic
    var playlistRepo = services.GetRequiredService<IPlaylistRepository>();
    var barPlaylistEntryRepo = services.GetRequiredService<IBarPlaylistEntryRepository>();

    if (!db.Bars.Any())
    {
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
        db.SaveChanges();
    }
}

// ---------------------------
// Middleware Pipeline
// ---------------------------

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

app.Run();