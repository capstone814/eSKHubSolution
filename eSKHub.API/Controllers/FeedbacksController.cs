using eSKHub.API.Data;
using eSKHub.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eSKHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbacksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FeedbacksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] Feedback feedback)
        {
            if (feedback == null)
            {
                return BadRequest("Invalid feedback data.");
            }

            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(feedback.Name) || string.IsNullOrWhiteSpace(feedback.Message))
                {
                    return BadRequest("Name and Message are required.");
                }

                // Reset ID to ensure EF creates a new record
                feedback.Id = 0;

                // Ensure default values
                if (feedback.DateSubmitted == default)
                {
                    feedback.DateSubmitted = DateTime.Now;
                }

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Feedback submitted successfully", Id = feedback.Id });
            }
            catch (Exception ex)
            {
                // In a real app, log this exception
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("user/{email}")]
        public async Task<IActionResult> GetUserFeedbacks(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required");

            var feedbacks = await _context.Feedbacks
                .Where(f => f.Email == email)
                .OrderByDescending(f => f.DateSubmitted)
                .ToListAsync();

            return Ok(feedbacks);
        }
    }
}
