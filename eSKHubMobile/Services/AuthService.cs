using System.Security.Cryptography;
using System.Text;

namespace eSKHubMobile.Services
{
    public class AuthService
    {
        private readonly OfflineDbService _offlineDb;
        private readonly ApiService _apiService;
        private readonly OfflineSyncService _syncService; // Added
        private string? _currentUsername;
        private string? _currentRole;
        private string? _currentFullname;
        private string? _currentPosition;
        private string? _currentArea;
        private string? _profilePath;

        public bool IsAuthenticated { get; private set; }
        public string? Username => _currentUsername;
        public string? Role => _currentRole;
        public string? Fullname => _currentFullname;
        public string? Position => _currentPosition;
        public string? Area => _currentArea;
        public string? ProfilePath => _profilePath;

        public event Action? OnAuthStateChanged;

        public AuthService(OfflineDbService offlineDb, ApiService apiService, OfflineSyncService syncService)
        {
            _offlineDb = offlineDb;
            _apiService = apiService;
            _syncService = syncService; // Added
        }

        public async Task<(bool success, string message)> LoginAsync(string username, string password)
        {
            var hashedPassword = HashPassword(password);
            var isOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

            Console.WriteLine($"[AuthService] Login attempt - Username: {username}, Online: {isOnline}");

            // ALWAYS try offline login first
            var offlineUser = await _offlineDb.GetUserAsync(username);

            if (offlineUser != null && offlineUser.Password == hashedPassword)
            {
                Console.WriteLine("[AuthService] Offline credentials valid");

                // Check role
                if (offlineUser.Role != "Council")
                {
                    return (false, "Access denied: Only Council members can use the mobile app.");
                }

                _currentUsername = username;
                _currentRole = offlineUser.Role;
                _currentFullname = offlineUser.Fullname;
                _currentPosition = offlineUser.Position;
                _currentArea = offlineUser.Area;
                _profilePath = offlineUser.LocalPath;
                IsAuthenticated = true;
                Preferences.Set("LoggedInUserArea", _currentArea);
                OnAuthStateChanged?.Invoke();

                return (true, "Login successful.");
            }

            // Offline credentials invalid - only try API if online
            if (!isOnline)
            {
                Console.WriteLine("[AuthService] Offline login failed and device is offline");
                return (false, "Invalid credentials. Internet connection required for first-time login.");
            }

            // Try API login for first-time users or password changes
            try
            {
                Console.WriteLine("[AuthService] Attempting online login via API...");
                var apiResponse = await _apiService.LoginAsync(username, password);

                if (apiResponse?.Success == true && apiResponse.User != null)
                {
                    Console.WriteLine("[AuthService] API login successful");

                    // Check role before saving
                    if (apiResponse.User.Role != "Council")
                    {
                        return (false, "Access denied: Only Council members can use the mobile app.");
                    }

                    // Save user credentials for offline use
                    await _offlineDb.SaveUserAsync(new Models.OfflineUser
                    {
                        Id = apiResponse.User.Id,
                        Username = apiResponse.User.Username,
                        Password = hashedPassword,
                        Email = apiResponse.User.Email,
                        Role = apiResponse.User.Role,
                        Fullname = apiResponse.User.Fullname,
                        Position = apiResponse.User.Position,
                        Area = apiResponse.User.Area,
                        Image = apiResponse.User.Image
                    });

                    // Syncing will be handled by Login.razor for better UX control

                    _currentUsername = apiResponse.User.Username;
                    _currentRole = apiResponse.User.Role;
                    _currentFullname = apiResponse.User.Fullname;
                    _currentPosition = apiResponse.User.Position;
                    _currentArea = apiResponse.User.Area;
                    _profilePath = null; // Will be available after first sync
                    IsAuthenticated = true;
                    Preferences.Set("LoggedInUserArea", _currentArea);
                    OnAuthStateChanged?.Invoke();

                    return (true, "Login successful.");
                }

                string apiMessage = apiResponse?.Message ?? "Unknown error from server";
                Console.WriteLine($"[AuthService] API login failed: {apiMessage}");
                return (false, $"Login failed: {apiMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthService] Login Error: {ex.Message}");
                return (false, $"An unexpected error occurred: {ex.Message}");
            }
        }

        public async Task<bool> SyncAnnouncementsAsync()
        {
            try
            {
                Console.WriteLine("[AuthService] Starting full data sync via OfflineSyncService...");
                var (success, message, userCount, announcementCount) = await _syncService.DownloadOfflinePackageAsync();

                if (success)
                {
                    Console.WriteLine($"[AuthService] Full sync completed successfully. {announcementCount} announcements synced.");
                    return true;
                }

                Console.WriteLine($"[AuthService] Full sync failed: {message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthService] Sync error: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            _currentUsername = null;
            _currentRole = null;
            _currentFullname = null;
            _currentPosition = null;
            _currentArea = null;
            IsAuthenticated = false;
            OnAuthStateChanged?.Invoke();
            await Task.CompletedTask;
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}