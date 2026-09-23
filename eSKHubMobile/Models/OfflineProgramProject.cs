using SQLite;

namespace eSKHubMobile.Models
{
    public class OfflineProgramProject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int RemoteId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Venue { get; set; }
        public decimal Budget { get; set; }
        public string? TargetBeneficiaries { get; set; }
        public string? Objectives { get; set; }
        public string? ExpectedOutcomes { get; set; }
        public string? Timeline { get; set; }
        public string? ProposedBy { get; set; }
        public string? ProposerPosition { get; set; }
        public string? ProposerArea { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
        public string? Partners { get; set; }
        public DateTime DateCreated { get; set; }
        public string? Image { get; set; }
        public string? LocalPath { get; set; }
        public string? AttachmentType { get; set; }
    }
}
