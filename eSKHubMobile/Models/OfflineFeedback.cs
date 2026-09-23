using SQLite;

namespace eSKHubMobile.Models
{
    public class OfflineFeedback
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Category { get; set; }
        public DateTime DateSubmitted { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public bool IsSynced { get; set; } = false;
    }
}
