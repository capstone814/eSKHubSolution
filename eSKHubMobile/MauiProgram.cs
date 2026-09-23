using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using eSKHubMobile.Data;
using eSKHubMobile.Services;
using Plugin.LocalNotification;

namespace eSKHubMobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                })
                .UseLocalNotification()
                .UseLocalNotification();

            // Add configuration
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("eSKHubMobile.appsettings.json");

            if (stream == null)
            {
                throw new InvalidOperationException("Could not find embedded resource 'eSKHubMobile.appsettings.json'");
            }

            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            builder.Configuration.AddConfiguration(config);

            // Register services
            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddSingleton<OfflineDbService>();
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<OfflineSyncService>(); // NEW
            builder.Services.AddScoped<AppDbContext>(sp =>
                new AppDbContext(sp.GetRequiredService<IConfiguration>()));
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<IDownloadService, DownloadService>();
            builder.Services.AddSingleton<IAppNotificationService, NotificationService>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();


            // Initialize offline database on startup
            Task.Run(async () =>
            {
                try
                {
                    var offlineDb = app.Services.GetRequiredService<OfflineDbService>();
                    var syncService = app.Services.GetRequiredService<OfflineSyncService>();

                    // Delay startup sync to allow UI and permissions to stabilize
                    await Task.Delay(5000);

                    Console.WriteLine("═══════════════════════════════════════════════════════");
                    Console.WriteLine("           eSKHub Mobile Starting");
                    Console.WriteLine("═══════════════════════════════════════════════════════");

                    // Check if we have offline data
                    var hasData = await syncService.HasValidOfflineDataAsync();

                    if (hasData)
                    {
                        Console.WriteLine("✓ Offline data found in cache");
                    }
                    else
                    {
                        Console.WriteLine("⚠ No offline data - will need to download on first login");
                    }

                    // Check if we can reach the API
                    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                    {
                        Console.WriteLine("✓ Internet connection available");

                        var packageInfo = await syncService.GetPackageInfoAsync();
                        if (packageInfo != null)
                        {
                            Console.WriteLine($"✓ API reachable - {packageInfo.UserCount} users, {packageInfo.AnnouncementCount} announcements available");
                            Console.WriteLine("[Startup] ↻ Starting background auto-sync...");
                            var (success, _, users, announcements) = await syncService.DownloadOfflinePackageAsync();
                            if (success)
                            {
                                Console.WriteLine($"[Startup] ✓ Auto-sync complete. {announcements} announcements synced.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("⚠ Cannot reach API - will use offline data");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠ No internet connection - offline mode only");
                    }

                    Console.WriteLine("═══════════════════════════════════════════════════════");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Startup error: {ex.Message}");
                }
            });

            return app;
        }
    }
}