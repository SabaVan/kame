using backend.Data;
using backend.Services;
using backend.Services.Interfaces;
using backend.Repositories;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using backend.Controllers;
using backend.Shared.Enums;
using backend.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Database connection
// ---------------------------
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "BarDb";

var connectionString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPassword};Database={dbName}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ---------------------------
// Dependency Injection for backend services
// ---------------------------
builder.Services.AddScoped<IBarService, SimpleBarService>();
builder.Services.AddScoped<IBarRepository, BarRepository>();
builder.Services.AddScoped<IBarUserEntryRepository, BarUserEntryRepository>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthController>();

builder.Services.AddScoped<ICreditService, CreditService>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddAutoMapper(typeof(Program));

// ---------------------------
// Add controllers, Swagger, CORS, session
// ---------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- Runtime seeding ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();


    // Seed Bars if none exist
    if (!db.Bars.Any())
    {
        var bar = new Bar { Name = "Kame Bar" };
        bar.SetState(BarState.Open);
        bar.SetSchedule(
            new DateTime(2025, 10, 17, 17, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 17, 22, 0, 0, DateTimeKind.Utc)
        );

        db.Bars.Add(bar);
        db.SaveChanges();
    }
}

// ---------------------------
// Middleware pipeline
// ---------------------------
app.UseCors("DevCors");
app.UseHttpsRedirection();
app.UseSession();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.Run();