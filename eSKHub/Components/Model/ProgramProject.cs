using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class ProgramProject
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [Required]
        public string Description { get; set; } = default!;

        [Required]
        public string Objectives { get; set; } = default!;

        [Required]
        [StringLength(500)]
        public string TargetBeneficiaries { get; set; } = default!;

        [Required]
        public string ExpectedOutcomes { get; set; } = default!;

        [Required]
        public decimal Budget { get; set; }

        [Required]
        [StringLength(200)]
        public string Timeline { get; set; } = default!;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = default!;

        [Required]
        [StringLength(300)]
        public string Venue { get; set; } = default!;

        public string? Partners { get; set; }

        public string? AttachmentType { get; set; }
        public byte[]? AttachmentData { get; set; }
        public string? AttachmentMimeType { get; set; }
        public string? Attachment { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateModified { get; set; }

        [Required]
        public string ProposedBy { get; set; } = default!;

        [Required]
        public string ProposerPosition { get; set; } = default!;

        [Required]
        public string ProposerArea { get; set; } = default!;

        [Required]
        public string Status { get; set; } = "Pending";

        public string? AdminFeedback { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }

        public bool IsPublished { get; set; } = false;

        [Required]
        public string Audience { get; set; } = "Public";

        public string? TargetBarangay { get; set; }

        public bool EditAccessRequested { get; set; } = false;
        public DateTime? EditAccessRequestedDate { get; set; }
        public bool EditAccessGranted { get; set; } = false;
        public DateTime? EditAccessGrantedDate { get; set; }
        public string? EditAccessGrantedBy { get; set; }
    }
}