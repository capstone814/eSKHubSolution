using SQLite;

namespace eSKHubMobile.Models
{
    public class OfflinePoll
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int RemoteId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Options { get; set; } = string.Empty;
        public string? Votes { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateExpires { get; set; }
        public bool IsActive { get; set; }
        public string? VoterIds { get; set; }
    }
}
