using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class PageContent
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string PageName { get; set; } = default!; // "ContactUs" or "PrivacyPolicy"

        [Required]
        public string Content { get; set; } = default!; // JSON string containing page content

        public DateTime DateModified { get; set; } = DateTime.Now;

        public string? ModifiedBy { get; set; }
    }
}