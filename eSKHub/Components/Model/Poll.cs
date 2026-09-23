using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class Poll
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Question { get; set; } = default!;

        [Required]
        public string Options { get; set; } = default!; // JSON array of options

        public string? Votes { get; set; } // JSON object: {"option1": 5, "option2": 3}

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime? DateExpires { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public string CreatedBy { get; set; } = default!;

        [Required]
        public string CreatorArea { get; set; } = default!;

        [Required]
        public string Audience { get; set; } = "Public"; // Public, Barangay

        public string? TargetBarangay { get; set; }

        public bool AllowMultipleVotes { get; set; } = false;

        public string? VoterIds { get; set; } // JSON array of user IDs who voted
    }
}