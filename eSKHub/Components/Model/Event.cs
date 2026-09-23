using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Description { get; set; } = default!;

        [Required]
        public DateTime EventDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [Required]
        [StringLength(300)]
        public string Location { get; set; } = default!;

        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = default!; // Meeting, Sports, Community, Workshop, etc.

        [Required]
        public string Organizer { get; set; } = default!;

        [Required]
        public string OrganizerPosition { get; set; } = default!;

        [Required]
        public string OrganizerArea { get; set; } = default!;

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime? DateModified { get; set; }

        public bool IsPublished { get; set; } = false;

        [Required]
        public string Audience { get; set; } = "Public";

        public string? TargetBarangay { get; set; }

        public string? Notes { get; set; }
    }
}