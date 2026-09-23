using SQLite;

namespace eSKHubMobile.Models
{
    public class OfflineEvent
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int RemoteId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string? Organizer { get; set; }
        public DateTime DateCreated { get; set; }
        public string? Audience { get; set; }
        public string? TargetBarangay { get; set; }
    }
}
