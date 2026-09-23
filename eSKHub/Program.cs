using Blazored.SessionStorage;
using eSKHub.Components;
using eSKHub.Components.Model;
using eSKHub.Components.Services;
using eSKHub.Model;
using eSKHub.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Enable Legacy Timestamp Behavior for PostgreSQL (Npgsql) to accept Local/Unspecified DateTime values
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddMudServices();
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddScoped<eSKHub.Components.Services.ThemeService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<UserStateService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<PageContentService>();
builder.Services.AddScoped<IMomSummarizerService, MomSummarizerService>();
builder.Services.AddScoped<ChatService>();

Action<DbContextOptionsBuilder> dbConfig = options =>
{
    var connStr = builder.Configuration.GetConnectionString("DatabaseConnectionString");
    if (!string.IsNullOrEmpty(connStr) && (connStr.StartsWith("postgres", StringComparison.OrdinalIgnoreCase) || connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase)))
    {
        var npgsqlConnStr = ConvertPostgresUriToConnectionString(connStr);
        options.UseNpgsql(npgsqlConnStr);
    }
    else
    {
        options.UseSqlServer(connStr);
    }
};

builder.Services.AddDbContext<AppDbContext>(dbConfig);
builder.Services.AddDbContextFactory<AppDbContext>(dbConfig, ServiceLifetime.Scoped);

var app = builder.Build();

// Create default admin user if database is empty
await EnsureDefaultAdminAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Method to create default admin user
static async Task EnsureDefaultAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

    using var db = await dbFactory.CreateDbContextAsync();

    // Ensure database and tables are created
    await db.Database.EnsureCreatedAsync();

    // Run T-SQL table patch only if using SQL Server provider
    if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
    {
        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [dbo].[ChatMessages](
                    [Id] [int] IDENTITY(1,1) NOT NULL,
                    [SenderUsername] [nvarchar](max) NOT NULL,
                    [ReceiverUsername] [nvarchar](max) NOT NULL,
                    [Message] [nvarchar](max) NOT NULL,
                    [Timestamp] [datetime2](7) NOT NULL,
                    [IsRead] [bit] NOT NULL,
                    [ReadAt] [datetime2](7) NULL,
                    CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([Id] ASC)
                )
            END
            ELSE
            BEGIN
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND name = 'ReadAt')
                BEGIN
                    ALTER TABLE [dbo].[ChatMessages] ADD [ReadAt] [datetime2](7) NULL;
                END
            END";
        await db.Database.ExecuteSqlRawAsync(createTableSql);
    }

    // Check if any users exist
    if (!await db.Users.AnyAsync())
    {
        // Hash the default password
        var hashedPassword = HashPassword("admin123");

        var defaultAdmin = new User
        {
            Username = "admin",
            Password = hashedPassword,
            Email = "admin@eskhub.local",
            Role = "Admin",
            Fullname = "System Administrator",
            Position = "System Admin",
            Area = "Bacacay" // Set appropriate default area
        };

        db.Users.Add(defaultAdmin);
        await db.SaveChangesAsync();

        Console.WriteLine("Default admin user created successfully!");
        Console.WriteLine("Username: admin");
        Console.WriteLine("Password: admin123");
    }
}

// Hash password method (same as used in your application)
static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}

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