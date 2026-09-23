using Microsoft.AspNetCore.Mvc;
using eSKHub.API.Services;
using eSKHub.API.Models;

namespace eSKHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OfflineSyncController : ControllerBase
    {
        private readonly IDataSyncService _dataSyncService;
        private readonly ILogger<OfflineSyncController> _logger;

        public OfflineSyncController(
            IDataSyncService dataSyncService,
            ILogger<OfflineSyncController> logger)
        {
            _dataSyncService = dataSyncService;
            _logger = logger;
        }

        /// <summary>
        /// Download complete offline data package for mobile app
        /// </summary>
        [HttpGet("download-package")]
        [ProducesResponseType(typeof(OfflineDataPackage), StatusCodes.Status200OK)]
        public async Task<ActionResult<OfflineDataPackage>> DownloadPackage()
        {
            _logger.LogInformation("Offline package download requested");

            try
            {
                var package = await _dataSyncService.GetOfflineDataPackageAsync();

                _logger.LogInformation(
                    "Offline package generated: {UserCount} users, {AnnouncementCount} announcements",
                    package.Users.Count,
                    package.Announcements.Count);

                return Ok(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating offline package");
                return StatusCode(500, new { error = "Failed to generate offline package" });
            }
        }

        /// <summary>
        /// Get package info without downloading
        /// </summary>
        [HttpGet("package-info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetPackageInfo()
        {
            try
            {
                var package = await _dataSyncService.GetOfflineDataPackageAsync();

                var info = new
                {
                    success = true,
                    userCount = package.Users.Count,
                    announcementCount = package.Announcements.Count,
                    generatedAt = package.GeneratedAt,
                    version = package.Version,
                    sizeEstimate = $"~{(package.Users.Count * 200 + package.Announcements.Count * 500) / 1024}KB"
                };

                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting package info");
                return StatusCode(500, new { error = "Failed to get package info" });
            }
        }

        /// <summary>
        /// Force refresh of offline cache
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RefreshCache()
        {
            _logger.LogInformation("Cache refresh requested");

            try
            {
                var package = await _dataSyncService.GetOfflineDataPackageAsync();

                return Ok(new
                {
                    success = true,
                    message = "Cache refreshed successfully",
                    userCount = package.Users.Count,
                    announcementCount = package.Announcements.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing cache");
                return StatusCode(500, new { error = "Failed to refresh cache" });
            }
        }
    }
}