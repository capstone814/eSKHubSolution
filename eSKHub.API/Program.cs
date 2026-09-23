// eSKHub.API/Program.cs
using Microsoft.EntityFrameworkCore;
using eSKHub.API.Data;
using eSKHub.API.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args
});

// Enable Legacy Timestamp Behavior for PostgreSQL (Npgsql) to accept Local/Unspecified DateTime values
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Disable file watching for appsettings (fixes inotify limit on Linux containers like Render)
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// --------------------
// Services
// --------------------

// Add controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy for mobile app
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileAppPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add DbContext (PostgreSQL for Neon cloud, SQL Server / SQLite fallback)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("DatabaseConnectionString");
    
    if (!string.IsNullOrEmpty(connStr) && (connStr.StartsWith("postgres", StringComparison.OrdinalIgnoreCase) || connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase)))
    {
        var npgsqlConnStr = ConvertPostgresUriToConnectionString(connStr);
        options.UseNpgsql(npgsqlConnStr);
    }
    else if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ||
             string.IsNullOrEmpty(connStr) || 
             connStr.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "eskhub_server.db");
        options.UseSqlite($"Data Source={dbPath}");
    }
    else
    {
        options.UseSqlServer(connStr);
    }
});

// Add custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IDataSyncService, DataSyncService>(); // NEW


var app = builder.Build();

// --------------------
// Middleware pipeline
// --------------------
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("MobileAppPolicy");
app.UseAuthorization();
app.MapControllers();

// --------------------
// Generate initial offline data cache (non-blocking)
// --------------------
_ = Task.Run(async () =>
{
    await Task.Delay(1000); // Give host time to start listening
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync(); // Auto-create tables for SQLite on Render

        // Seed default Council user if empty
        if (!await db.Users.AnyAsync())
        {
            logger.LogInformation("Seeding default Council user accounts...");
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedPassword = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("password123")));

            db.Users.AddRange(
                new eSKHub.API.Models.User
                {
                    Username = "council",
                    Password = hashedPassword,
                    Fullname = "Council Member",
                    Position = "SK Member",
                    Role = "Council",
                    Area = "Barangay 1 - Poblacion",
                    Email = "council@eskhub.gov.ph"
                },
                new eSKHub.API.Models.User
                {
                    Username = "skmanager",
                    Password = hashedPassword,
                    Fullname = "John Vincent Bobier",
                    Position = "SK Federation Manager",
                    Role = "Council",
                    Area = "Barangay 1 - Poblacion",
                    Email = "manager@eskhub.gov.ph"
                }
            );
            await db.SaveChangesAsync();
        }

        // Seed default Announcement if empty
        if (!await db.Announcements.AnyAsync())
        {
            db.Announcements.Add(new eSKHub.API.Models.Announcement
            {
                Subject = "Welcome to eSKHub Mobile",
                Body = "eSKHub offline synchronization and notifications are now active.",
                IsPublished = true,
                DateCreated = DateTime.Now,
                Author = "John Vincent Bobier",
                Audience = "Public"
            });
            await db.SaveChangesAsync();
        }

        // Seed default Event if empty
        if (!await db.Events.AnyAsync())
        {
            db.Events.Add(new eSKHub.API.Models.Event
            {
                Title = "Youth Assembly Meeting",
                Description = "General assembly meeting for all youth members.",
                EventDate = DateTime.Today.AddDays(2),
                StartTime = new TimeSpan(17, 0, 0),
                EndTime = new TimeSpan(20, 0, 0),
                Location = "Barangay Hall",
                EventType = "Meeting",
                Organizer = "John Vincent Bobier",
                IsPublished = true,
                Audience = "Public"
            });
            await db.SaveChangesAsync();
        }

        var dataSyncService = scope.ServiceProvider.GetRequiredService<IDataSyncService>();
        logger.LogInformation("Generating initial offline data cache...");
        var package = await dataSyncService.GetOfflineDataPackageAsync();
        logger.LogInformation(
            "✓ Offline data cache ready: {UserCount} users, {AnnouncementCount} announcements",
            package?.Users?.Count ?? 0,
            package?.Announcements?.Count ?? 0);
    }
    catch (Exception ex)
    {
        logger.LogWarning("Notice: Database initial sync skipped/pending on startup: {Message}", ex.Message);
    }
});

// --------------------
// Test endpoint
// --------------------
app.MapGet("/", () => "eSKHub API is running!");

// --------------------
// Logging
// --------------------
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("           eSKHub API Started Successfully");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine($"HTTP:  http://0.0.0.0:5251");
Console.WriteLine($"HTTPS: https://0.0.0.0:7299");
Console.WriteLine($"Swagger: https://localhost:7299/swagger");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("Offline Sync Endpoints:");
Console.WriteLine("  GET  /api/OfflineSync/download-package");
Console.WriteLine("  GET  /api/OfflineSync/package-info");
Console.WriteLine("  POST /api/OfflineSync/refresh");
Console.WriteLine("═══════════════════════════════════════════════════════");

app.Run();

static string ConvertPostgresUriToConnectionString(string connStr)
{
    if (!connStr.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        return connStr;

    try
    {
        var uri = new Uri(connStr);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo.Length > 0 ? userInfo[0] : "";
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
    }
    catch
    {
        return connStr;
    }
}