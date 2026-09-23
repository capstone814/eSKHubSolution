using SQLite;

namespace eSKHubMobile.Models
{
    [Table("announcements")]
    public class OfflineAnnouncement
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int RemoteId { get; set; }

        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Author { get; set; }
        public int Priority { get; set; }
        public DateTime DateCreated { get; set; }
        public string? Attachment { get; set; }
        public string? AttachmentType { get; set; }
        public string? LocalPath { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }
}
