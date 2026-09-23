// eSKHub/Components/Model/Feedback.cs
using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class Feedback
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [StringLength(20)]
        public string? ContactNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = default!;

        [Required]
        public string Message { get; set; } = default!;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "General";

        public DateTime DateSubmitted { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public string? AdminNotes { get; set; }

        public DateTime? DateRead { get; set; }

        // Change to support multiple barangays (comma-separated)
        public bool IsForwarded { get; set; } = false;
        public string? ForwardedToBarangays { get; set; } // Comma-separated list
        public DateTime? ForwardedDate { get; set; }

        // JSON string to store read status per user: {"username1": true, "username2": false}
        public string? ReadByUsers { get; set; }

        // JSON string to store solved status per barangay: {"Barangay1": true, "Barangay2": false}
        public string? SolvedByBarangays { get; set; }
    }
}