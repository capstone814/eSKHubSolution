using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        public string Subject { get; set; } = default!;

        [Required]
        [StringLength(160, ErrorMessage = "Short description cannot exceed 160 characters")]
        public string ShortDescription { get; set; } = default!;

        [Required]
        public string Body { get; set; } = default!;

        public string? Attachment { get; set; }

        public string? AttachmentType { get; set; }

        public byte[]? AttachmentData { get; set; }
        public string? AttachmentMimeType { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime? DateModified { get; set; }

        public bool IsPublished { get; set; } = false;

        public string? Author { get; set; }

        public string? AuthorArea { get; set; }

        public string? AuthorPosition { get; set; }

        [Required]
        public string Audience { get; set; } = "Public";

        public string? TargetBarangay { get; set; }
    }
}