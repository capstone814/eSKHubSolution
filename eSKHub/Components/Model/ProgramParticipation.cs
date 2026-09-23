using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class ProgramParticipation
    {
        public int Id { get; set; }

        [Required]
        public int ProgramId { get; set; }

        [Required]
        public int YouthId { get; set; }

        [Required]
        public string YouthName { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string YouthEmail { get; set; } = default!;

        public string? YouthContact { get; set; }

        [Required]
        public string YouthBarangay { get; set; } = default!;

        public DateTime DateRegistered { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; } = "Registered"; // Registered, Attended, Cancelled

        public string? Notes { get; set; }

        // Navigation properties
        public ProgramProject? Program { get; set; }
        public Youth? Youth { get; set; }
    }
}