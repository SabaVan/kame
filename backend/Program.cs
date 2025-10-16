using backend.Data;
using backend.Services;
using backend.Services.Interfaces;
using backend.Repositories;
using backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
var username = Environment.GetEnvironmentVariable("DB_USER");
var org_id = Environment.GetEnvironmentVariable("ORG_ID");
// Read the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") + $"; Username={username}" + $";Password={password}";
var connectionStringIPv4 = builder.Configuration.GetConnectionString("IPv4Connection") + $"; Username={username + "." + org_id}" + $";Password={password}";
connectionString = connectionString.Replace("<ORG_ID>", org_id);
string bestConnection;
try
{
    // try IPv6 connection
    var testConn = new Npgsql.NpgsqlConnection(connectionString);
    testConn.Open();
    testConn.Close();
    Console.WriteLine("Connected via IPv6!");
    bestConnection = connectionString;
}
catch (Npgsql.NpgsqlException ex)
{
    // Check if it's a network-level failure (host unreachable, no route, etc.)
    if (ex.InnerException is System.Net.Sockets.SocketException socketEx)
    {
        Console.WriteLine($"IPv6 failed: {socketEx.Message}. Falling back to pooler/IPv4...");
        bestConnection = connectionStringIPv4;
    }
    else
    {
        // Some other Npgsql error (e.g., authentication)
        throw; // rethrow it
    }
}


// Configure EF Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(bestConnection));

// Register services
builder.Services.AddScoped<IBarService, SimpleBarService>();
builder.Services.AddScoped<IBarRepository, BarRepository>();

builder.Services.AddAutoMapper(typeof(Program)); // scans for Profiles



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

// Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
