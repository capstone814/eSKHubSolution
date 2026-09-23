using Microsoft.AspNetCore.Mvc;
using eSKHub.API.Models;
using eSKHub.API.Services;

namespace eSKHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user for mobile app access
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Login response with user data</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Invalid request data."
                });
            }

            _logger.LogInformation("Login attempt for user: {Username}", request.Username);

            var response = await _authService.AuthenticateAsync(request);

            if (response.Success)
            {
                _logger.LogInformation("Login successful for user: {Username}", request.Username);
                return Ok(response);
            }

            _logger.LogWarning("Login failed for user: {Username}. Reason: {Message}",
                request.Username, response.Message);
            return Ok(response); // Return 200 with success=false for client to handle
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}