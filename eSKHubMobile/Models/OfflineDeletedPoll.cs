using SQLite;

namespace eSKHubMobile.Models
{
    public class OfflineDeletedPoll
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int RemotePollId { get; set; }
        public DateTime DeletedAt { get; set; }
        public bool IsSynced { get; set; }
    }
}
