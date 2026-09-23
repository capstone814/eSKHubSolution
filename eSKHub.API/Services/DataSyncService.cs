// eSKHub.API/Services/DataSyncService.cs
using Microsoft.EntityFrameworkCore;
using eSKHub.API.Data;
using eSKHub.API.Models;
using System.Text.Json;

namespace eSKHub.API.Services
{
    public interface IDataSyncService
    {
        Task<string> GenerateOfflineDataPackageAsync();
        Task<OfflineDataPackage> GetOfflineDataPackageAsync();
    }

    public class DataSyncService : IDataSyncService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataSyncService> _logger;
        private readonly string _cacheFilePath;
        private OfflineDataPackage? _cachedData;
        private DateTime _lastCacheTime;

        public DataSyncService(AppDbContext context, ILogger<DataSyncService> logger)
        {
            _context = context;
            _logger = logger;
            _cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "offline_data.json");
            _lastCacheTime = DateTime.MinValue;
        }

        public async Task<OfflineDataPackage> GetOfflineDataPackageAsync()
        {
            // Return cached data if less than 5 minutes old
            if (_cachedData != null && (DateTime.UtcNow - _lastCacheTime).TotalMinutes < 5)
            {
                _logger.LogInformation("Returning cached offline data package");
                return _cachedData;
            }

            _logger.LogInformation("Generating fresh offline data package");

            try
            {
                var users = await _context.Users
                    .Where(u => u.Role == "Council")
                    .ToListAsync();

                var userDtos = users.Select(u => new OfflineUserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Password = u.Password,
                    Email = u.Email,
                    Role = u.Role,
                    Fullname = u.Fullname,
                    Position = u.Position,
                    Area = u.Area,
                    Image = u.ImageData != null ? $"data:{u.ImageMimeType ?? "image/jpeg"};base64,{Convert.ToBase64String(u.ImageData)}" : null
                }).ToList();

                var announcements = await _context.Announcements
                    .Where(a => a.IsPublished)
                    .OrderByDescending(a => a.DateCreated)
                    .ToListAsync();

                var announcementDtos = announcements.Select(a => new OfflineAnnouncementDto
                {
                    Id = a.Id,
                    Subject = a.Subject,
                    Body = a.Body,
                    Author = a.Author,
                    DateCreated = a.DateCreated,
                    DateModified = a.DateModified,
                    Attachment = a.AttachmentData != null ? $"data:{a.AttachmentMimeType ?? "image/jpeg"};base64,{Convert.ToBase64String(a.AttachmentData)}" : null,
                    AttachmentType = a.AttachmentType,
                    Audience = a.Audience,
                    TargetBarangay = a.TargetBarangay
                }).ToList();

                // Get events
                var events = await _context.Events
                    .Where(e => e.IsPublished)
                    .OrderByDescending(e => e.EventDate)
                    .Select(e => new OfflineEventDto
                    {
                        Id = e.Id,
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
                    })
                    .ToListAsync();

                // Get programs
                var programs = await _context.ProgramsProjects
                    .Where(p => p.IsPublished && p.Status == "Approved")
                    .OrderByDescending(p => p.DateCreated)
                    .ToListAsync();

                var programDtos = programs.Select(p => new OfflineProgramDto
                {
                    Id = p.Id,
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
                    Image = p.AttachmentData != null ? $"data:{p.AttachmentMimeType ?? "image/jpeg"};base64,{Convert.ToBase64String(p.AttachmentData)}" : null,
                    AttachmentType = p.AttachmentType,
                    DateCreated = p.DateCreated
                }).ToList();

                var news = await _context.NewsArticles
                    .Where(n => n.IsPublished)
                    .OrderByDescending(n => n.DateCreated)
                    .ToListAsync();

                var newsDtos = news.Select(n => new OfflineNewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Summary = n.Summary,
                    Content = n.Content,
                    NewsType = n.NewsType,
                    DateCreated = n.DateCreated,
                    Author = n.Author,
                    Image = n.AttachmentData != null ? $"data:{n.AttachmentMimeType ?? "image/jpeg"};base64,{Convert.ToBase64String(n.AttachmentData)}" : null
                }).ToList();

                // Get polls
                var polls = await _context.Polls
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.DateCreated)
                    .Select(p => new OfflinePollDto
                    {
                        Id = p.Id,
                        Question = p.Question,
                        Options = p.Options,
                        Votes = p.Votes,
                        DateCreated = p.DateCreated,
                        DateExpires = p.DateExpires,
                        IsActive = p.IsActive,
                        VoterIds = p.VoterIds
                    })
                    .ToListAsync();

                var package = new OfflineDataPackage
                {
                    Users = userDtos,
                    Announcements = announcementDtos,
                    Events = events,
                    Programs = programDtos,
                    News = newsDtos,
                    Polls = polls,
                    GeneratedAt = DateTime.UtcNow,
                    Version = "1.0"
                };

                // Cache in memory
                _cachedData = package;
                _lastCacheTime = DateTime.UtcNow;

                // Save to file for persistence
                await SaveToFileAsync(package);

                _logger.LogInformation(
                    "Generated offline data package: {UserCount} users, {AnnouncementCount} announcements",
                    users.Count, announcements.Count);

                return package;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating offline data package");
                throw;
            }
        }

        public async Task<string> GenerateOfflineDataPackageAsync()
        {
            var package = await GetOfflineDataPackageAsync();
            return JsonSerializer.Serialize(package, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        private async Task SaveToFileAsync(OfflineDataPackage package)
        {
            try
            {
                var json = JsonSerializer.Serialize(package, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_cacheFilePath, json);
                _logger.LogInformation("Offline data package saved to: {FilePath}", _cacheFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save offline data package to file");
            }
        }

        public async Task<OfflineDataPackage?> LoadFromFileAsync()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    var json = await File.ReadAllTextAsync(_cacheFilePath);
                    var package = JsonSerializer.Deserialize<OfflineDataPackage>(json);
                    _logger.LogInformation("Loaded offline data package from file");
                    return package;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load offline data package from file");
            }

            return null;
        }
    }

    // DTOs for offline data package
    public class OfflineDataPackage
    {
        public List<OfflineUserDto> Users { get; set; } = new();
        public List<OfflineAnnouncementDto> Announcements { get; set; } = new();
        public List<OfflineEventDto> Events { get; set; } = new();
        public List<OfflineProgramDto> Programs { get; set; } = new();
        public List<OfflineNewsDto> News { get; set; } = new();
        public List<OfflinePollDto> Polls { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public string Version { get; set; } = "1.0";
    }

    public class OfflineUserDto
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

    public class OfflineAnnouncementDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Author { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public string? Attachment { get; set; }
        public string? AttachmentType { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }

    public class OfflineEventDto
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

    public class OfflineProgramDto
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

    public class OfflineNewsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? NewsType { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string? Author { get; set; }
        public string? Image { get; set; }
    }

    public class OfflinePollDto
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
}