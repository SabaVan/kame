using backend.Data;
using backend.Services;
using backend.Services.Interfaces;
using backend.Repositories;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using backend.Controllers;


var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Database connection
// ---------------------------
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "myappdb";

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
        policy.WithOrigins("http://localhost:5173") // frontend origin
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
});

var app = builder.Build();

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