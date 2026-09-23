using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using eSKHubMobile.Models;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace eSKHubMobile.Services
{
    public class OfflineSyncService
    {
        private readonly ApiService _apiService;
        private readonly OfflineDbService _offlineDb;
        private readonly IAppNotificationService _notificationService;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly SemaphoreSlim _syncSemaphore = new(1, 1);

        public OfflineSyncService(
            ApiService apiService,
            OfflineDbService offlineDb,
            IAppNotificationService notificationService,
            IConfiguration configuration)
        {
            _apiService = apiService;
            _offlineDb = offlineDb;
            _notificationService = notificationService;

            var apiSettings = configuration.GetSection("ApiSettings");

            // Determine platform-specific settings
            string platformKey = GetPlatformKey();
            var platformSettings = apiSettings.GetSection(platformKey);

            // Get base URL from configuration
            _baseUrl = platformSettings["BaseUrl"]
                       ?? apiSettings.GetSection("Default")["BaseUrl"]
                       ?? "http://localhost:5251/api";

            // Get certificate validation setting
            bool allowSelfSignedCerts = bool.Parse(
                platformSettings["AllowSelfSignedCertificates"]
                ?? apiSettings.GetSection("Default")["AllowSelfSignedCertificates"]
                ?? "false");

            // Get timeout
            int timeoutSeconds = int.Parse(apiSettings["TimeoutSeconds"] ?? "30");

            // Configure HttpClient handler
            var handler = new HttpClientHandler();

            if (allowSelfSignedCerts)
            {
                handler.ServerCertificateCustomValidationCallback =
                    (message, cert, chain, errors) => true;
            }

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            Console.WriteLine($"[OfflineSyncService] Initialized with base URL: {_baseUrl}");

            // Subscribe to connectivity changes
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

            // Start background upload/sync processes
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                _ = Task.Run(UploadPendingDataAsync);
            }

            // Start periodic sync (1 minute)
            _ = Task.Run(StartPeriodicSyncAsync);
        }

        private async Task StartPeriodicSyncAsync()
        {
            Console.WriteLine("[OfflineSyncService] 🕒 Automatic 1-minute sync started.");
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync())
            {
                try
                {
                    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                    {
                        Console.WriteLine("[OfflineSyncService] 🕒 Periodic check triggered...");
                        await DownloadOfflinePackageAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OfflineSyncService] 🕒 Periodic sync error: {ex.Message}");
                }
            }
        }

        private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                Console.WriteLine("[OfflineSyncService] 🌐 Network restored. Triggering full sync...");
                await UploadPendingDataAsync();
                await DownloadOfflinePackageAsync();
            }
        }

        private static string GetPlatformKey()
        {
#if ANDROID
            return "Android";
#elif IOS
            return "iOS";
#else
            return "Default";
#endif
        }

        /// <summary>
        /// Download complete offline data package from API
        /// </summary>
        public async Task<(bool success, string message, int userCount, int announcementCount)> DownloadOfflinePackageAsync(bool isSilent = false)
        {
            try
            {
                if (!isSilent)
                    Console.WriteLine("[OfflineSyncService] Starting manual offline package download...");
                else
                    Console.WriteLine("[OfflineSyncService] Starting background FCM sync...");

                // Check if online
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    return (false, "No internet connection", 0, 0);
                }

                // Download package
                var url = $"{_baseUrl}/OfflineSync/download-package";
                Console.WriteLine($"[OfflineSyncService] Downloading from: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"Server returned: {response.StatusCode}", 0, 0);
                }

                var package = await response.Content.ReadFromJsonAsync<OfflineDataPackageDto>();

                if (package == null)
                {
                    return (false, "Failed to parse data package", 0, 0);
                }

                Console.WriteLine($"[OfflineSyncService] Package received: {package.Users.Count} users, {package.Announcements.Count} announcements");

                // Save users
                var offlineUsers = new List<OfflineUser>();
                foreach (var u in package.Users)
                {
                    var user = new OfflineUser
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Password = u.Password,
                        Email = u.Email,
                        Role = u.Role,
                        Fullname = u.Fullname,
                        Position = u.Position,
                        Area = u.Area
                    };

                    // Save image to file and store path
                    if (!string.IsNullOrEmpty(u.Image))
                    {
                        user.LocalPath = await SaveBase64ToFileAsync(u.Image, "users", $"user_{u.Id}.jpg");
                    }

                    offlineUsers.Add(user);
                }
                await _offlineDb.SaveItemsAsync(offlineUsers);

                // Load existing state for ALL types BEFORE any saves
                // Capture Dictionaries for change detection
                var existingAnnouncementsDict = (await _offlineDb.GetItemsAsync<OfflineAnnouncement>()).ToDictionary(x => x.RemoteId);
                var existingNewsDict = (await _offlineDb.GetItemsAsync<OfflineNewsArticle>()).ToDictionary(x => x.RemoteId);
                var existingProgramsDict = (await _offlineDb.GetItemsAsync<OfflineProgramProject>()).ToDictionary(x => x.RemoteId);
                var existingEventsDict = (await _offlineDb.GetItemsAsync<OfflineEvent>()).ToDictionary(x => x.RemoteId);

                // Save announcements
                var offlineAnnouncements = new List<OfflineAnnouncement>();
                foreach (var a in package.Announcements)
                {
                    var announcement = new OfflineAnnouncement
                    {
                        RemoteId = a.Id,
                        Subject = a.Subject,
                        Body = a.Body,
                        Author = a.Author,
                        Priority = a.Priority,
                        DateCreated = a.DateCreated,
                        AttachmentType = a.AttachmentType,
                        Audience = a.Audience,
                        TargetBarangay = a.TargetBarangay
                    };

                    if (!string.IsNullOrEmpty(a.Attachment))
                    {
                        string ext = a.AttachmentType?.Contains("pdf") == true ? "pdf" : "jpg";
                        announcement.LocalPath = await SaveBase64ToFileAsync(a.Attachment, "announcements", $"ann_{a.Id}.{ext}");
                    }

                    offlineAnnouncements.Add(announcement);
                }
                await _offlineDb.SaveItemsAsync(offlineAnnouncements);

                // Notify for new/updated announcements
                int newAnnounces = 0;
                int updatedAnnounces = 0;
                OfflineAnnouncement? lastAnnounce = null;

                // Load logged in user's Barangay to filter notifications
                string? loggedInUserArea = Preferences.Get("LoggedInUserArea", null);

                foreach (var a in offlineAnnouncements)
                {
                    bool isNew = !existingAnnouncementsDict.ContainsKey(a.RemoteId);
                    bool hasChanged = false;

                    if (isNew)
                    {
                        lastAnnounce = a;
                    }
                    else
                    {
                        var old = existingAnnouncementsDict[a.RemoteId];
                        hasChanged = old.Subject != a.Subject || old.Body != a.Body;
                        if (hasChanged)
                        {
                            lastAnnounce = a;
                        }
                    }

                    // Determine if the announcement targets this device's currently logged-in user
                    bool isTargetedToDevice = string.IsNullOrEmpty(a.Audience) || 
                                              a.Audience.Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                                              (a.Audience.Equals("Barangay", StringComparison.OrdinalIgnoreCase) && 
                                               !string.IsNullOrEmpty(a.TargetBarangay) && 
                                               a.TargetBarangay.Equals(loggedInUserArea, StringComparison.OrdinalIgnoreCase));

                    if (isTargetedToDevice)
                    {
                        if (isNew)
                        {
                            newAnnounces++;
                        }
                        else if (hasChanged)
                        {
                            updatedAnnounces++;
                        }
                    }
                }

                if (newAnnounces > 0 || updatedAnnounces > 0)
                {
                    string title = updatedAnnounces > 0 && newAnnounces == 0 ? "Announcement Updated" : "New Announcement";
                    string msg = string.Empty;

                    if (newAnnounces + updatedAnnounces == 1 && lastAnnounce != null)
                    {
                        var snippet = lastAnnounce.Body != null && lastAnnounce.Body.Length > 60 ? lastAnnounce.Body.Substring(0, 57) + "..." : lastAnnounce.Body;
                        msg = $"{lastAnnounce.Subject}: {snippet}";
                    }
                    else
                    {
                        msg = $"Found {newAnnounces} new and {updatedAnnounces} updated announcements.";
                    }
                    await _notificationService.ShowNotificationAsync(title, msg, 2001);
                }

                // Save Events
                var offlineEvents = package.Events.Select(e => new OfflineEvent
                {
                    RemoteId = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Location = e.Location,
                    EventType = e.EventType,
                    Organizer = e.Organizer,
                    DateCreated = e.DateCreated,
                    Audience = e.Audience,
                    TargetBarangay = e.TargetBarangay
                }).ToList();
                await _offlineDb.SaveItemsAsync(offlineEvents);

                // Notify and Schedule for events 
                int newEventCount = 0;
                int updatedEventCount = 0;
                OfflineEvent? lastEv = null;

                // Use the already loaded loggedInUserArea to filter notifications

                foreach (var ev in offlineEvents)
                {
                    bool isNew = !existingEventsDict.ContainsKey(ev.RemoteId);
                    bool hasChanged = false;

                    if (isNew)
                    {
                        lastEv = ev;
                    }
                    else
                    {
                        var old = existingEventsDict[ev.RemoteId];
                        hasChanged = old.EventDate != ev.EventDate ||
                                     old.StartTime != ev.StartTime ||
                                     old.EndTime != ev.EndTime ||
                                     old.Title != ev.Title ||
                                     old.Location != ev.Location ||
                                     old.Description != ev.Description;

                        if (hasChanged)
                        {
                            lastEv = ev;
                        }
                    }

                    // Determine if the event targets this device's currently logged-in user
                    bool isTargetedToDevice = string.IsNullOrEmpty(ev.Audience) || 
                                              ev.Audience.Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                                              (ev.Audience.Equals("Barangay", StringComparison.OrdinalIgnoreCase) && 
                                               !string.IsNullOrEmpty(ev.TargetBarangay) && 
                                               ev.TargetBarangay.Equals(loggedInUserArea, StringComparison.OrdinalIgnoreCase));

                    if (isTargetedToDevice)
                    {
                        if (isNew)
                        {
                            newEventCount++;
                        }
                        else if (hasChanged)
                        {
                            updatedEventCount++;
                        }

                        if (isNew || hasChanged)
                        {
                            await _notificationService.ScheduleEventReminders(ev);
                        }
                    }
                    else
                    {
                        // Cancel any previously scheduled reminders if the event target changed away from user's Barangay
                        _notificationService.CancelEventReminders(ev.RemoteId);
                    }
                }

                if (newEventCount > 0 || updatedEventCount > 0)
                {
                    string title = updatedEventCount > 0 && newEventCount == 0 ? "Event Details Updated" : "Upcoming Events";
                    string msg = lastEv != null
                        ? $"{lastEv.Title} at {lastEv.Location} on {lastEv.EventDate:MMM dd}"
                        : $"Found {newEventCount} new events and {updatedEventCount} updates.";

                    await _notificationService.ShowNotificationAsync(title, msg, 2002);
                }

                // Save Programs
                var offlinePrograms = new List<OfflineProgramProject>();
                foreach (var p in package.Programs)
                {
                    var program = new OfflineProgramProject
                    {
                        RemoteId = p.Id,
                        Title = p.Title,
                        Description = p.Description,
                        Category = p.Category,
                        Status = p.Status,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Venue = p.Venue,
                        Budget = p.Budget,
                        TargetBeneficiaries = p.TargetBeneficiaries,
                        Objectives = p.Objectives,
                        ExpectedOutcomes = p.ExpectedOutcomes,
                        Timeline = p.Timeline,
                        ProposedBy = p.ProposedBy,
                        ProposerPosition = p.ProposerPosition,
                        ProposerArea = p.ProposerArea,
                        Audience = p.Audience,
                        TargetBarangay = p.TargetBarangay,
                        Partners = p.Partners,
                        AttachmentType = p.AttachmentType,
                        Image = p.Image,
                        DateCreated = p.DateCreated
                    };

                    if (!string.IsNullOrEmpty(p.Image))
                    {
                        program.LocalPath = await SaveBase64ToFileAsync(p.Image, "programs", $"prog_{p.Id}.jpg");
                    }

                    offlinePrograms.Add(program);
                }
                await _offlineDb.SaveItemsAsync(offlinePrograms);

                // Notify for programs
                int newProgs = 0;
                int updatedProgs = 0;
                OfflineProgramProject? lastProg = null;

                foreach (var p in offlinePrograms)
                {
                    if (!existingProgramsDict.ContainsKey(p.RemoteId))
                    {
                        newProgs++;
                        lastProg = p;
                    }
                    else if (existingProgramsDict[p.RemoteId].Title != p.Title || existingProgramsDict[p.RemoteId].Status != p.Status)
                    {
                        updatedProgs++;
                        lastProg = p;
                    }
                }

                if (newProgs > 0 || updatedProgs > 0)
                {
                    string title = updatedProgs > 0 && newProgs == 0 ? "Program Updated" : "Programs & Projects";
                    string msg = (newProgs + updatedProgs == 1 && lastProg != null)
                        ? $"{(updatedProgs > 0 ? "Updated" : "New")} {lastProg.Category}: {lastProg.Title}"
                        : $"Found {newProgs} new and {updatedProgs} updated programs.";
                    await _notificationService.ShowNotificationAsync(title, msg, 400);
                }

                // Save News
                var offlineNews = new List<OfflineNewsArticle>();
                foreach (var n in package.News)
                {
                    var article = new OfflineNewsArticle
                    {
                        RemoteId = n.Id,
                        Title = n.Title,
                        Summary = n.Summary,
                        Content = n.Content,
                        NewsType = n.NewsType,
                        DateCreated = n.DateCreated,
                        Author = n.Author
                    };

                    if (!string.IsNullOrEmpty(n.Image))
                    {
                        article.LocalPath = await SaveBase64ToFileAsync(n.Image, "news", $"news_{n.Id}.jpg");
                    }

                    offlineNews.Add(article);
                }
                await _offlineDb.SaveItemsAsync(offlineNews);

                // Notify for news
                int newNewsCount = 0;
                int updatedNewsCount = 0;
                OfflineNewsArticle? lastN = null;

                foreach (var n in offlineNews)
                {
                    if (!existingNewsDict.ContainsKey(n.RemoteId))
                    {
                        newNewsCount++;
                        lastN = n;
                    }
                    else if (existingNewsDict[n.RemoteId].Title != n.Title || existingNewsDict[n.RemoteId].Summary != n.Summary)
                    {
                        updatedNewsCount++;
                        lastN = n;
                    }
                }

                if ((newNewsCount > 0 || updatedNewsCount > 0) && !isSilent)
                {
                    string title = updatedNewsCount > 0 && newNewsCount == 0 ? "News Updated" : "eSKHub News";
                    string msg = string.Empty;
                    if (newNewsCount + updatedNewsCount == 1 && lastN != null)
                    {
                        var snippet = lastN.Summary != null && lastN.Summary.Length > 60 ? lastN.Summary.Substring(0, 57) + "..." : lastN.Summary;
                        msg = $"{lastN.Title}: {snippet}";
                    }
                    else
                    {
                        msg = $"Found {newNewsCount} new and {updatedNewsCount} updated articles.";
                    }
                    await _notificationService.ShowNotificationAsync(title, msg, 500);
                }

                // Save Polls
                var offlinePolls = package.Polls.Select(p => new OfflinePoll
                {
                    RemoteId = p.Id,
                    Question = p.Question,
                    Options = p.Options,
                    Votes = p.Votes,
                    DateCreated = p.DateCreated,
                    DateExpires = p.DateExpires,
                    IsActive = p.IsActive,
                    VoterIds = p.VoterIds
                }).ToList();
                await _offlineDb.SaveItemsAsync(offlinePolls);

                Console.WriteLine("[OfflineSyncService] ✓ Package saved to local database");

                // Persist last sync time
                var syncTime = DateTime.Now.ToString("MMM dd, yyyy HH:mm");
                Preferences.Set("LastSyncTime", syncTime);
                Console.WriteLine($"[OfflineSyncService] ✓ Last sync time saved: {syncTime}");

                // Removed bulk success notification to reduce noise as requested by user
                // await _notificationService.ShowSyncSuccessNotificationAsync(package.Users.Count, package.Announcements.Count);

                return (true, "Data synchronized successfully", package.Users.Count, package.Announcements.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OfflineSyncService] Sync Error (Muted): {ex.Message}");
                if (!isSilent)
                {
                    // await _notificationService.ShowSyncErrorNotificationAsync(ex.Message);
                }
                return (false, $"Error: {ex.Message}", 0, 0);
            }
        }

        public async Task<PackageInfo?> GetPackageInfoAsync()
        {
            try
            {
                var url = $"{_baseUrl}/OfflineSync/package-info";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PackageInfo>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OfflineSyncService] Failed to get package info: {ex.Message}");
            }

            return null;
        }

        public async Task<bool> HasValidOfflineDataAsync()
        {
            try
            {
                var announcements = await _offlineDb.GetAnnouncementsAsync();
                return announcements.Any();
            }
            catch
            {
                return false;
            }
        }
        public async Task UploadPendingDataAsync()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            if (!await _apiService.TestConnectionAsync()) return;

            if (!await _syncSemaphore.WaitAsync(0)) return;

            try
            {
                Console.WriteLine("[OfflineSyncService] Starting background upload of pending data...");

                // Sync Feedback
                var pendingFeedback = await _offlineDb.GetUnsyncedFeedbackAsync();
                if (pendingFeedback.Any())
                {
                    Console.WriteLine($"[OfflineSyncService] Found {pendingFeedback.Count} pending feedback items.");
                    foreach (var feedback in pendingFeedback)
                    {
                        var success = await _apiService.SubmitFeedbackAsync(feedback);
                        if (success)
                        {
                            feedback.IsSynced = true;
                            await _offlineDb.SaveItemAsync(feedback);
                            Console.WriteLine($"[OfflineSyncService] Feedback {feedback.Id} synced successfully.");
                        }
                    }
                }

                // Sync Poll Answers
                var pendingAnswers = await _offlineDb.GetUnsyncedPollAnswersAsync();
                if (pendingAnswers.Any())
                {
                    Console.WriteLine($"[OfflineSyncService] Found {pendingAnswers.Count} pending poll answers.");
                    foreach (var answer in pendingAnswers)
                    {
                        // We need the user's ID for the vote. We'll use username for now as ID if ID is not available.
                        // Actually, AuthService should have the real ID if synced.
                        var success = await _apiService.SubmitVoteAsync(answer.RemotePollId, answer.SelectedOptionIndex, answer.Username);
                        if (success)
                        {
                            answer.IsSynced = true;
                            await _offlineDb.SaveItemAsync(answer);
                            Console.WriteLine($"[OfflineSyncService] Poll answer {answer.Id} synced successfully.");
                        }
                    }
                }

                // Sync Poll Deletions
                var pendingDeletions = await _offlineDb.GetUnsyncedDeletedPollsAsync();
                if (pendingDeletions.Any())
                {
                    Console.WriteLine($"[OfflineSyncService] Found {pendingDeletions.Count} pending poll deletions.");
                    foreach (var deletion in pendingDeletions)
                    {
                        var success = await DeletePollOnServerAsync(deletion.RemotePollId);
                        if (success)
                        {
                            deletion.IsSynced = true;
                            await _offlineDb.SaveItemAsync(deletion);
                            Console.WriteLine($"[OfflineSyncService] Poll deletion {deletion.Id} synced successfully.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OfflineSyncService] Error syncing pending data: {ex.Message}");
            }

            finally
            {
                _syncSemaphore.Release();
            }
        }

        private async Task<bool> DeletePollOnServerAsync(int pollId)
        {
            try
            {
                var success = await _apiService.DeletePollAsync(pollId);
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OfflineSyncService] Error deleting poll on server: {ex.Message}");
                return false;
            }
        }

        public async Task RefreshPollsAsync()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            try
            {
                Console.WriteLine("[OfflineSyncService] Refreshing polls from server...");
                // We reuse the main sync logic but focus on polls if we want specialized behavior, 
                // but for now, DownloadOfflinePackageAsync is robust.
                await DownloadOfflinePackageAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OfflineSyncService] Error refreshing polls: {ex.Message}");
            }
        }
        public async Task SyncFeedbackHistoryAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return;
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            try
            {
                var serverFeedbacks = await _apiService.GetUserFeedbacksAsync(email);
                if (serverFeedbacks != null)
                {
                    await _offlineDb.DeleteSyncedFeedbackAsync(email);

                    var pending = await _offlineDb.GetUnsyncedFeedbackAsync();
                    var pendingIds = pending.Select(p => p.Id).ToHashSet();

                    foreach (var f in serverFeedbacks)
                    {
                        if (pendingIds.Contains(f.Id)) continue;

                        f.IsSynced = true;
                        await _offlineDb.SaveItemAsync(f);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OfflineSyncService] History sync failed: {ex.Message}");
            }
        }

        private async Task<string?> SaveBase64ToFileAsync(string? base64Data, string subfolder, string fileName)
        {
            if (string.IsNullOrEmpty(base64Data)) return null;

            try
            {
                // Remove data URI prefix if present (e.g., "data:image/jpeg;base64,")
                string pureBase64 = base64Data;
                if (base64Data.Contains(","))
                {
                    pureBase64 = base64Data.Split(',')[1];
                }

                byte[] bytes = Convert.FromBase64String(pureBase64);

                // REDUCE IMAGE SIZE FOR MOBILE PERFORMANCE
                // This prevents "BAD ALLOC" errors in GPU drivers by ensuring 
                // textures aren't massive.
                try
                {
                    bytes = ResizeImage(bytes, 800);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OfflineSyncService] Image optimization skipped: {ex.Message}");
                }

                string dirPath = Path.Combine(FileSystem.AppDataDirectory, "images", subfolder);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                string filePath = Path.Combine(dirPath, fileName);
                await File.WriteAllBytesAsync(filePath, bytes);

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OfflineSyncService] Failed to save image to file: {ex.Message}");
                return null;
            }
        }

        private byte[] ResizeImage(byte[] imageBytes, float maxDimension)
        {
            try
            {
                using var stream = new MemoryStream(imageBytes);
                var image = PlatformImage.FromStream(stream);

                if (image == null) return imageBytes;

                float width = image.Width;
                float height = image.Height;

                if (width > maxDimension || height > maxDimension)
                {
                    float ratio = Math.Min(maxDimension / width, maxDimension / height);
                    int newWidth = (int)(width * ratio);
                    int newHeight = (int)(height * ratio);

                    // Creating a downsized version
                    using var downsampled = image.Downsize(newWidth, newHeight, true);
                    return downsampled.AsBytes(); // Efficiently get bytes back
                }

                return imageBytes;
            }
            catch
            {
                // If anything goes wrong with graphics engine, return original bytes as fallback
                return imageBytes;
            }
        }
    }

    // DTOs matching API response
    public class OfflineDataPackageDto
    {
        public List<OfflineUserDtoApi> Users { get; set; } = new();
        public List<OfflineAnnouncementDtoApi> Announcements { get; set; } = new();
        public List<OfflineEventDtoApi> Events { get; set; } = new();
        public List<OfflineProgramDtoApi> Programs { get; set; } = new();
        public List<OfflineNewsDtoApi> News { get; set; } = new();
        public List<OfflinePollDtoApi> Polls { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public string Version { get; set; } = "1.0";
    }

    public class OfflineUserDtoApi
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Fullname { get; set; }
        public string? Position { get; set; }
        public string? Area { get; set; }
        public string? Image { get; set; }
    }

    public class OfflineAnnouncementDtoApi
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Author { get; set; }
        public int Priority { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public string? Attachment { get; set; }
        public string? AttachmentType { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }

    public class OfflineEventDtoApi
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string? Organizer { get; set; }
        public DateTime DateCreated { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }

    public class OfflineProgramDtoApi
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Venue { get; set; }
        public decimal Budget { get; set; }
        public string? TargetBeneficiaries { get; set; }
        public string? Objectives { get; set; }
        public string? ExpectedOutcomes { get; set; }
        public string? Timeline { get; set; }
        public string? ProposedBy { get; set; }
        public string? ProposerPosition { get; set; }
        public string? ProposerArea { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
        public string? Partners { get; set; }
        public string? Image { get; set; }
        public string? AttachmentType { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class OfflineNewsDtoApi
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string NewsType { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string? Author { get; set; }
        public string? Image { get; set; }
    }

    public class OfflinePollDtoApi
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Options { get; set; } = string.Empty;
        public string? Votes { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateExpires { get; set; }
        public bool IsActive { get; set; }
        public string? VoterIds { get; set; }
    }

    public class PackageInfo
    {
        public bool Success { get; set; }
        public int UserCount { get; set; }
        public int AnnouncementCount { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Version { get; set; } = string.Empty;
        public string SizeEstimate { get; set; } = string.Empty;
    }
}