using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eSKHub.API.Data;
using eSKHub.API.Models;
using System.Text.Json;

namespace eSKHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PollsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PollsController> _logger;

        public PollsController(AppDbContext context, ILogger<PollsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("{id}/vote")]
        public async Task<IActionResult> SubmitVote(int id, [FromQuery] int optionIndex, [FromQuery] string userId)
        {
            _logger.LogInformation("Vote submission for poll {PollId}, index {OptionIndex} by user {UserId}", id, optionIndex, userId);

            var poll = await _context.Polls.FindAsync(id);
            if (poll == null) return NotFound("Poll not found");
            if (!poll.IsActive || (poll.DateExpires.HasValue && poll.DateExpires < DateTime.Now))
                return BadRequest("Poll is closed");

            // Parse VoterIds (JSON List or Comma-separated)
            List<string> voterIds;
            try
            {
                voterIds = string.IsNullOrEmpty(poll.VoterIds)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(poll.VoterIds) ?? new List<string>();
            }
            catch
            {
                voterIds = (poll.VoterIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (voterIds.Contains(userId))
                return BadRequest("User already voted");

            // Parse Options (JSON List or Pipe-separated)
            List<string> options;
            try
            {
                options = JsonSerializer.Deserialize<List<string>>(poll.Options) ?? new List<string>();
            }
            catch
            {
                options = poll.Options.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (optionIndex < 0 || optionIndex >= options.Count)
                return BadRequest("Invalid option index");

            var selectedOption = options[optionIndex];

            // Parse Votes (JSON Dictionary or Comma-separated indices)
            Dictionary<string, int> votesDict = new Dictionary<string, int>();
            try
            {
                if (!string.IsNullOrEmpty(poll.Votes) && poll.Votes.Trim().StartsWith("{"))
                {
                    votesDict = JsonSerializer.Deserialize<Dictionary<string, int>>(poll.Votes) ?? new Dictionary<string, int>();
                }
                else if (!string.IsNullOrEmpty(poll.Votes))
                {
                    // Legacy comma separated - convert to names
                    var counts = poll.Votes.Split(',').Select(s => int.TryParse(s.Trim(), out var v) ? v : 0).ToList();
                    for (int i = 0; i < Math.Min(counts.Count, options.Count); i++)
                    {
                        votesDict[options[i]] = counts[i];
                    }
                }
            }
            catch
            {
                votesDict = new Dictionary<string, int>();
            }

            if (votesDict.ContainsKey(selectedOption)) votesDict[selectedOption]++;
            else votesDict[selectedOption] = 1;

            // Save back as JSON
            poll.Votes = JsonSerializer.Serialize(votesDict);

            voterIds.Add(userId);
            poll.VoterIds = JsonSerializer.Serialize(voterIds);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, votes = poll.Votes });
        }
    }
}
