using SQLite;

namespace eSKHubMobile.Models
{
    public class OfflineNewsArticle
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int RemoteId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string NewsType { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string? Author { get; set; }
        public string? Image { get; set; }
        public string? LocalPath { get; set; }
    }
}
