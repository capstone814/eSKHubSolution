using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace eSKHubMobile.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        public string Subject { get; set; } = default!;

        [Required]
        public string Body { get; set; } = default!;

        public string? Attachment { get; set; }

        public string? AttachmentType { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public DateTime? DateModified { get; set; }

        public bool IsPublished { get; set; } = false;

        public string? Author { get; set; }

        public string? Category { get; set; }

        public int Priority { get; set; } = 0;
    }
}
