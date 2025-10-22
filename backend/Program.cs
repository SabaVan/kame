using backend.Data;
using backend.Services;
using backend.Services.Interfaces;
using backend.Repositories;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load .env variables
Env.Load();

// Read the connection strings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Configure EF Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register services
builder.Services.AddScoped<IBarService, SimpleBarService>();
builder.Services.AddScoped<IBarRepository, BarRepository>();
builder.Services.AddScoped<IBarUserEntryRepository, BarUserEntryRepository>();

builder.Services.AddScoped<IPlaylistService, SimplePlaylistService>();
builder.Services.AddScoped<IPlaylistRepository, PlaylistRepository>();

builder.Services.AddScoped<ISongRepository, ExternalAPISongRepository>();
builder.Services.AddScoped<ISongService, SongService>();

builder.Services.AddAutoMapper(typeof(Program));

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
  options.AddPolicy("DevCors", policy =>
      policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Middleware
app.UseCors("DevCors");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.Run();