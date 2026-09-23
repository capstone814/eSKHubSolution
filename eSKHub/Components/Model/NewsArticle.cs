using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class NewsArticle
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = default!;

        [Required]
        [StringLength(160, ErrorMessage = "Summary cannot exceed 160 characters")]
        public string Summary { get; set; } = default!;

        [Required]
        public string Content { get; set; } = default!;

        [Required]
        public string NewsType { get; set; } = default!; // Breaking, Feature, Community, Event, Achievement

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
    }
}