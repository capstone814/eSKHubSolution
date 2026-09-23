using Microsoft.AspNetCore.Mvc;
using eSKHub.API.Models;
using eSKHub.API.Services;

namespace eSKHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;
        private readonly ILogger<AnnouncementsController> _logger;

        public AnnouncementsController(
            IAnnouncementService announcementService,
            ILogger<AnnouncementsController> logger)
        {
            _announcementService = announcementService;
            _logger = logger;
        }

        /// <summary>
        /// Get all published announcements for mobile sync
        /// </summary>
        /// <returns>List of published announcements</returns>
        [HttpGet("sync")]
        [ProducesResponseType(typeof(SyncResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<SyncResponse>> SyncAnnouncements()
        {
            _logger.LogInformation("Sync request received at {Timestamp}", DateTime.UtcNow);

            var response = await _announcementService.GetPublishedAnnouncementsAsync();

            if (response.Success)
            {
                _logger.LogInformation("Sync completed: {Count} announcements retrieved",
                    response.Announcements.Count);
            }
            else
            {
                _logger.LogError("Sync failed: {Message}", response.Message);
            }

            return Ok(response);
        }

        /// <summary>
        /// Get announcements with optional filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<AnnouncementDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<AnnouncementDto>>>> GetAnnouncements(
            [FromQuery] int? limit = null)
        {
            var response = await _announcementService.GetPublishedAnnouncementsAsync();

            var announcements = response.Announcements;

            if (limit.HasValue && limit.Value > 0)
            {
                announcements = announcements.Take(limit.Value).ToList();
            }

            return Ok(new ApiResponse<List<AnnouncementDto>>
            {
                Success = true,
                Message = $"Retrieved {announcements.Count} announcements.",
                Data = announcements
            });
        }
    }
}