using Microsoft.EntityFrameworkCore;
using eSKHub.API.Data;
using eSKHub.API.Models;

namespace eSKHub.API.Services
{
    public interface IAnnouncementService
    {
        Task<SyncResponse> GetPublishedAnnouncementsAsync();
    }

    public class AnnouncementService : IAnnouncementService
    {
        private readonly AppDbContext _context;

        public AnnouncementService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SyncResponse> GetPublishedAnnouncementsAsync()
        {
            try
            {
                var announcements = await _context.Announcements
                    .Where(a => a.IsPublished)
                    .OrderByDescending(a => a.DateCreated)
                    .AsNoTracking()
                    .Select(a => new AnnouncementDto
                    {
                        Id = a.Id,
                        Subject = a.Subject,
                        Body = a.Body,
                        Author = a.Author,
                        DateCreated = a.DateCreated,
                        DateModified = a.DateModified,
                        Attachment = a.Attachment,
                        AttachmentType = a.AttachmentType
                    })
                    .ToListAsync();

                return new SyncResponse
                {
                    Success = true,
                    Message = $"Retrieved {announcements.Count} announcements.",
                    Announcements = announcements,
                    SyncTimestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new SyncResponse
                {
                    Success = false,
                    Message = $"Error retrieving announcements: {ex.Message}",
                    Announcements = new List<AnnouncementDto>(),
                    SyncTimestamp = DateTime.UtcNow
                };
            }
        }
    }
}