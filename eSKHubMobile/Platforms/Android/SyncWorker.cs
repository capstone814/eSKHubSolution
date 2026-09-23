using Android.Content;
using AndroidX.Work;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using System.Net.Http.Json;
using SQLite;

namespace eSKHubMobile.Platforms.Android
{
    /// <summary>
    /// Android WorkManager worker – runs in the background even when the app is closed.
    /// Scheduled as a PeriodicWorkRequest (minimum 15 minutes on Android).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416")]
    public class SyncWorker : Worker
    {
        // ──────────────────────────────────────────────────────────────────────
        // Constants
        // ──────────────────────────────────────────────────────────────────────
        public const string WORK_NAME = "eSKHub_BackgroundSync";
        public const string CHANNEL_ID = "event_reminders";
        private const string DB_FILENAME = "eskhub_offline.db3";
        // Production:
        // private const string API_BASE_URL = "https://eskhub.onrender.com/api";
        // Localhost (Android Emulator):
        private const string API_BASE_URL = "http://192.168.254.104:5251/api";

        // ──────────────────────────────────────────────────────────────────────
        // Constructor
        // ──────────────────────────────────────────────────────────────────────
        public SyncWorker(Context context, WorkerParameters workerParams)
            : base(context, workerParams) { }

        // ──────────────────────────────────────────────────────────────────────
        // Entry point
        // ──────────────────────────────────────────────────────────────────────
        public override Result DoWork()
        {
            try
            {
                Console.WriteLine("[SyncWorker] ▶ Background sync triggered by WorkManager.");
                
                // Check network connectivity
                var connectivityManager = (global::Android.Net.ConnectivityManager?)
                    ApplicationContext.GetSystemService(Context.ConnectivityService);
                
                var network = connectivityManager?.ActiveNetwork;
                var capabilities = connectivityManager?.GetNetworkCapabilities(network);
                bool isOnline = capabilities?.HasCapability(
                    global::Android.Net.NetCapability.Internet) == true;
                
                if (!isOnline)
                {
                    Console.WriteLine("[SyncWorker] ✗ No internet – skipping sync.");
                    return Result.InvokeSuccess(); // Don't retry; wait for next schedule
                }

                // Run async work synchronously inside Worker.DoWork()
                Task.Run(async () => await RunSyncAsync()).GetAwaiter().GetResult();
                return Result.InvokeSuccess();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncWorker] ❌ Fatal error: {ex.Message}");
                return Result.InvokeFailure();
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Core sync logic
        // ──────────────────────────────────────────────────────────────────────
        private async Task RunSyncAsync()
        {
            global::Android.Util.Log.Info("eSKHubSync", "RunSyncAsync started.");

            // 1. Download the offline package from the API
            BackgroundPackageDto? package = null;
            try
            {
                package = await FetchPackageAsync();
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("eSKHubSync", $"FetchPackageAsync crashed: {ex.Message}\n{ex.StackTrace}");
            }

            if (package == null)
            {
                global::Android.Util.Log.Error("eSKHubSync", "Package download failed or returned null.");
                return;
            }

            global::Android.Util.Log.Info("eSKHubSync", $"Package downloaded successfully. Announcements: {package.Announcements?.Count ?? 0}, Events: {package.Events?.Count ?? 0}, News: {package.News?.Count ?? 0}");

            // 2. Open local SQLite database
            string dbPath = string.Empty;
            try
            {
                var externalDir = global::Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
                var filesDir = global::Android.App.Application.Context.FilesDir?.AbsolutePath;
                var mauiDir = FileSystem.AppDataDirectory;
                
                global::Android.Util.Log.Info("eSKHubSync", $"Paths - External: {externalDir}, Files: {filesDir}, MAUI: {mauiDir}");
                
                dbPath = Path.Combine(externalDir ?? filesDir ?? mauiDir, DB_FILENAME);

                // Also try the standard MAUI AppData path
                var mauiDbPath = Path.Combine(mauiDir, DB_FILENAME);
                global::Android.Util.Log.Info("eSKHubSync", $"Checking if DB exists at MAUI path: {mauiDbPath}");
                if (File.Exists(mauiDbPath))
                {
                    dbPath = mauiDbPath;
                }
                else
                {
                    global::Android.Util.Log.Warn("eSKHubSync", $"DB not found at MAUI path. Trying: {dbPath}");
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("eSKHubSync", $"Error resolving database path: {ex.Message}");
            }

            SQLiteAsyncConnection db;
            try
            {
                db = new SQLiteAsyncConnection(dbPath);

                // Ensure tables exist — safe no-op if columns already match,
                // and auto-creates on a fresh install before the app is ever opened.
                await db.CreateTableAsync<LocalAnnouncement>();
                await db.CreateTableAsync<LocalEvent>();
                await db.CreateTableAsync<LocalNews>();

                global::Android.Util.Log.Info("eSKHubSync", $"DB ready at: {dbPath}");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("eSKHubSync", $"Failed to open/create SQLite DB: {ex.Message}");
                return;
            }

            try
            {
                // 3. Check for new announcements
                global::Android.Util.Log.Info("eSKHubSync", "Checking announcements...");
                await CheckAnnouncementsAsync(db, package.Announcements ?? []);

                // 4. Check for new/changed events
                global::Android.Util.Log.Info("eSKHubSync", "Checking events...");
                await CheckEventsAsync(db, package.Events ?? []);

                // 5. Check for new news
                global::Android.Util.Log.Info("eSKHubSync", "Checking news...");
                await CheckNewsAsync(db, package.News ?? []);

                // 6. Persist last background sync time
                Preferences.Set("LastBgSyncTime", DateTime.Now.ToString("MMM dd, yyyy HH:mm"));
                global::Android.Util.Log.Info("eSKHubSync", "Background sync complete.");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("eSKHubSync", $"Error running checks or saving data: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Fetch package from API
        // ──────────────────────────────────────────────────────────────────────
        private async Task<BackgroundPackageDto?> FetchPackageAsync()
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
                };

                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
                var url = $"{API_BASE_URL}/OfflineSync/download-package";
                Console.WriteLine($"[SyncWorker] Fetching: {url}");
                
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[SyncWorker] ✗ API returned {response.StatusCode}");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<BackgroundPackageDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncWorker] ✗ Fetch error: {ex.Message}");
                return null;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Change-detection helpers
        // ──────────────────────────────────────────────────────────────────────
        private async Task CheckAnnouncementsAsync(SQLiteAsyncConnection db,
            List<BgAnnouncementDto> incoming)
        {
            try
            {
                var existing = await db.Table<LocalAnnouncement>().ToListAsync();
                var existingMap = existing.GroupBy(x => x.RemoteId).ToDictionary(g => g.Key, g => g.First());

                int newCount = 0;
                BgAnnouncementDto? lastNew = null;

                // Load logged in user's Barangay to filter notifications
                string? loggedInUserArea = Preferences.Get("LoggedInUserArea", null);

                global::Android.Util.Log.Info("eSKHubSync", $"CheckAnnouncements: User Barangay is: '{loggedInUserArea}'. Existing remote IDs in DB: {string.Join(",", existingMap.Keys)}");

                foreach (var a in incoming)
                {
                    // Determine if the announcement targets this device's currently logged-in user
                    bool isTargetedToDevice = string.IsNullOrEmpty(a.Audience) || 
                                              a.Audience.Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                                              (a.Audience.Equals("Barangay", StringComparison.OrdinalIgnoreCase) && 
                                               !string.IsNullOrEmpty(a.TargetBarangay) && 
                                               a.TargetBarangay.Equals(loggedInUserArea, StringComparison.OrdinalIgnoreCase));

                    global::Android.Util.Log.Info("eSKHubSync", $"Announcement #{a.Id} ('{a.Subject}'): Audience='{a.Audience}', Target='{a.TargetBarangay}', isTargetedToDevice={isTargetedToDevice}");

                    if (!isTargetedToDevice)
                    {
                        continue;
                    }

                    bool alreadyExists = existingMap.ContainsKey(a.Id);
                    global::Android.Util.Log.Info("eSKHubSync", $"Announcement #{a.Id} alreadyExists in local DB={alreadyExists}");

                    if (!alreadyExists)
                    {
                        newCount++;
                        lastNew = a;
                    }
                }

                global::Android.Util.Log.Info("eSKHubSync", $"CheckAnnouncements complete. newCount={newCount}");

                if (newCount > 0)
                {
                    string title = "New Announcement";
                    string msg = newCount == 1 && lastNew != null
                        ? $"{lastNew.Subject}"
                        : $"{newCount} new announcements available.";

                    await ShowNotificationAsync(title, msg, 2001);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncWorker] Announcements check error: {ex.Message}");
            }
        }

        private async Task CheckEventsAsync(SQLiteAsyncConnection db,
            List<BgEventDto> incoming)
        {
            try
            {
                var existing = await db.Table<LocalEvent>().ToListAsync();
                var existingMap = existing.GroupBy(x => x.RemoteId).ToDictionary(g => g.Key, g => g.First());

                int newCount = 0;
                int updatedCount = 0;
                BgEventDto? lastEv = null;

                // Load logged in user's Barangay to filter notifications
                string? loggedInUserArea = Preferences.Get("LoggedInUserArea", null);

                foreach (var ev in incoming)
                {
                    // Determine if the event targets this device's currently logged-in user
                    bool isTargetedToDevice = string.IsNullOrEmpty(ev.Audience) || 
                                              ev.Audience.Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                                              (ev.Audience.Equals("Barangay", StringComparison.OrdinalIgnoreCase) && 
                                               !string.IsNullOrEmpty(ev.TargetBarangay) && 
                                               ev.TargetBarangay.Equals(loggedInUserArea, StringComparison.OrdinalIgnoreCase));

                    if (!isTargetedToDevice)
                    {
                        continue;
                    }

                    if (!existingMap.ContainsKey(ev.Id))
                    {
                        newCount++;
                        lastEv = ev;
                    }
                    else
                    {
                        var old = existingMap[ev.Id];
                        if (old.Title != ev.Title || old.EventDate != ev.EventDate || old.Location != ev.Location)
                        {
                            updatedCount++;
                            lastEv = ev;
                        }
                    }
                }

                if (newCount > 0 || updatedCount > 0)
                {
                    string title = updatedCount > 0 && newCount == 0
                        ? "Event Details Updated"
                        : "Upcoming Events";

                    string msg = lastEv != null
                        ? $"{lastEv.Title} on {lastEv.EventDate:MMM dd} at {lastEv.Location}"
                        : $"{newCount} new, {updatedCount} updated events.";

                    await ShowNotificationAsync(title, msg, 2002);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncWorker] Events check error: {ex.Message}");
            }
        }

        private async Task CheckNewsAsync(SQLiteAsyncConnection db,
            List<BgNewsDto> incoming)
        {
            try
            {
                var existing = await db.Table<LocalNews>().ToListAsync();
                var existingMap = existing.ToDictionary(x => x.RemoteId);

                int newCount = 0;
                BgNewsDto? lastN = null;

                foreach (var n in incoming)
                {
                    if (!existingMap.ContainsKey(n.Id))
                    {
                        newCount++;
                        lastN = n;
                    }
                }

                if (newCount > 0)
                {
                    string title = "eSKHub News";
                    string msg = newCount == 1 && lastN != null
                        ? lastN.Title
                        : $"{newCount} new news articles.";

                    await ShowNotificationAsync(title, msg, 2003);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncWorker] News check error: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Native Android notification (no Plugin.LocalNotification dependency
        // from background context — use native NotificationManager directly)
        // ──────────────────────────────────────────────────────────────────────
        private Task ShowNotificationAsync(string title, string body, int notifId)
        {
            try
            {
                var ctx = ApplicationContext;

                // Build intent to open the app when tapped
                var intent = ctx.PackageManager!.GetLaunchIntentForPackage(ctx.PackageName!);
                intent?.AddFlags(global::Android.Content.ActivityFlags.ClearTop |
                                 global::Android.Content.ActivityFlags.SingleTop);

                var pendingIntent = global::Android.App.PendingIntent.GetActivity(
                    ctx, notifId, intent,
                    global::Android.App.PendingIntentFlags.UpdateCurrent |
                    global::Android.App.PendingIntentFlags.Immutable);

                var builder = new global::Android.App.Notification.Builder(ctx, CHANNEL_ID)
                    .SetContentTitle(title)
                    .SetContentText(body)
                    .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                    .SetAutoCancel(true)
                    .SetContentIntent(pendingIntent)
                    .SetPriority((int)global::Android.App.NotificationPriority.High)
                    .SetVisibility(global::Android.App.NotificationVisibility.Public);

                var notifManager = (global::Android.App.NotificationManager?)
                    ctx.GetSystemService(Context.NotificationService);

                notifManager?.Notify(notifId, builder.Build());
                Console.WriteLine($"[SyncWorker] 🔔 Notification sent – [{notifId}] {title}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncWorker] ❌ Notification error: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Lightweight DTOs for the background package (mirrors the main app DTOs)
    // ──────────────────────────────────────────────────────────────────────────

    public class BackgroundPackageDto
    {
        public List<BgAnnouncementDto> Announcements { get; set; } = [];
        public List<BgEventDto> Events { get; set; } = [];
        public List<BgNewsDto> News { get; set; } = [];
    }

    public class BgAnnouncementDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }

    public class BgEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }

    public class BgNewsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Minimal SQLite table models (must match column names in the main app DB)
    // ──────────────────────────────────────────────────────────────────────────

    [SQLite.Table("OfflineAnnouncement")]
    public class LocalAnnouncement
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }
        public int RemoteId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? Body { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }

    [SQLite.Table("OfflineEvent")]
    public class LocalEvent
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }
        public int RemoteId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }

    [SQLite.Table("OfflineNewsArticle")]
    public class LocalNews
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }
        public int RemoteId { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
