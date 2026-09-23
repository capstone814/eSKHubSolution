using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using eSKHubMobile.Models;

namespace eSKHubMobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiService(IConfiguration configuration)
        {
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
                Console.WriteLine("[ApiService] WARNING: Self-signed certificates are allowed (development only)");
            }

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            Console.WriteLine($"[ApiService] Initialized for {platformKey}");
            Console.WriteLine($"[ApiService] Base URL: {_baseUrl}");
            Console.WriteLine($"[ApiService] Timeout: {timeoutSeconds}s");
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

        // Test API connection
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Console.WriteLine($"[ApiService] Testing connection to {_baseUrl}/auth/health");
                var response = await _httpClient.GetAsync($"{_baseUrl}/auth/health");
                Console.WriteLine($"[ApiService] Health check status: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Health check failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ApiService] Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        // Login
        public async Task<ApiLoginResponse?> LoginAsync(string username, string password)
        {
            try
            {
                Console.WriteLine($"[ApiService] Attempting login for user: {username}");

                var request = new ApiLoginRequest
                {
                    Username = username,
                    Password = password
                };

                var url = $"{_baseUrl}/auth/login";
                Console.WriteLine($"[ApiService] Posting to: {url}");

                var response = await _httpClient.PostAsJsonAsync(url, request);
                Console.WriteLine($"[ApiService] Response status: {response.StatusCode}");

                if (response.Content != null)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] Raw response content: {content}");

                    try
                    {
                        var result = JsonSerializer.Deserialize<ApiLoginResponse>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (result != null)
                        {
                            Console.WriteLine($"[ApiService] Deserialized successfully. Success: {result.Success}");
                            return result;
                        }
                        else
                        {
                            return new ApiLoginResponse
                            {
                                Success = false,
                                Message = $"Login failed. API returned unrecognized content. HTTP {(int)response.StatusCode}: {content}"
                            };
                        }
                    }
                    catch (JsonException jex)
                    {
                        return new ApiLoginResponse
                        {
                            Success = false,
                            Message = $"Login failed. Invalid JSON from server. HTTP {(int)response.StatusCode}: {content}. Error: {jex.Message}"
                        };
                    }
                }

                return new ApiLoginResponse
                {
                    Success = false,
                    Message = $"Login failed. Empty response from server. HTTP {(int)response.StatusCode}"
                };
            }
            catch (HttpRequestException hex)
            {
                Console.WriteLine($"[ApiService] HttpRequestException: {hex.Message}");
                if (hex.InnerException != null)
                {
                    Console.WriteLine($"[ApiService] Inner exception: {hex.InnerException.Message}");
                }

                return new ApiLoginResponse
                {
                    Success = false,
                    Message = $"Network error: Connection failure. Ensure API is running and accessible. {hex.Message}"
                };
            }
            catch (TaskCanceledException tex)
            {
                Console.WriteLine($"[ApiService] TaskCanceledException: {tex.Message}");
                return new ApiLoginResponse
                {
                    Success = false,
                    Message = "Request timeout. The server took too long to respond."
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Unexpected error: {ex.Message}");
                Console.WriteLine($"[ApiService] Stack trace: {ex.StackTrace}");
                return new ApiLoginResponse
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
        }

        // Sync Announcements
        public async Task<ApiSyncResponse?> SyncAnnouncementsAsync()
        {
            try
            {
                Console.WriteLine($"[ApiService] Syncing announcements");
                var response = await _httpClient.GetAsync($"{_baseUrl}/announcements/sync");
                Console.WriteLine($"[ApiService] Sync response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiSyncResponse>();
                    Console.WriteLine($"[ApiService] Synced {result?.Announcements?.Count ?? 0} announcements");
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Sync error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ApiService] Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        // Submit Feedback
        public async Task<bool> SubmitFeedbackAsync(OfflineFeedback feedback)
        {
            try
            {
                var url = $"{_baseUrl}/feedbacks";
                Console.WriteLine($"[ApiService] Submitting feedback. URL: {url}");
                Console.WriteLine($"[ApiService] Payload: {JsonSerializer.Serialize(feedback)}");

                var response = await _httpClient.PostAsJsonAsync(url, feedback);
                Console.WriteLine($"[ApiService] Feedback submission status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] Server Error Response: {errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Feedback submission error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ApiService] Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<List<OfflineFeedback>?> GetUserFeedbacksAsync(string email)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/feedbacks/user/{email}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<OfflineFeedback>>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error getting user feedbacks: {ex.Message}");
                return null;
            }
        }

        // Submit Vote
        public async Task<bool> SubmitVoteAsync(int pollId, int optionIndex, string userId)
        {
            try
            {
                var url = $"{_baseUrl}/polls/{pollId}/vote?optionIndex={optionIndex}&userId={userId}";
                Console.WriteLine($"[ApiService] Submitting vote. URL: {url}");

                var response = await _httpClient.PostAsync(url, null);
                Console.WriteLine($"[ApiService] Vote submission status: {response.StatusCode}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Vote submission error: {ex.Message}");
                return false;
            }
        }

        // Delete Poll
        public async Task<bool> DeletePollAsync(int pollId)
        {
            try
            {
                var url = $"{_baseUrl}/polls/{pollId}";
                Console.WriteLine($"[ApiService] Deleting poll. URL: {url}");

                var response = await _httpClient.DeleteAsync(url);
                Console.WriteLine($"[ApiService] Poll deletion status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] Server Error Response: {errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Poll deletion error: {ex.Message}");
                return false;
            }
        }
    }


    // API DTOs
    public class ApiLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ApiLoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ApiUserDto? User { get; set; }
    }

    public class ApiUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Fullname { get; set; }
        public string? Position { get; set; }
        public string? Area { get; set; }
        public string? Image { get; set; }
    }

    public class ApiAnnouncementDto
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
    }

    public class ApiSyncResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ApiAnnouncementDto> Announcements { get; set; } = new();
        public DateTime SyncTimestamp { get; set; }
    }
}