using SQLite;

namespace eSKHubMobile.Models
{
    public class OfflinePollAnswer
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int RemotePollId { get; set; }
        public int SelectedOptionIndex { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsSynced { get; set; }
        public DateTime AnsweredAt { get; set; }
    }
}
