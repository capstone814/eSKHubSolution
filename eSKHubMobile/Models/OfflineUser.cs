using SQLite;

namespace eSKHubMobile.Models
{
    [Table("users")]
    public class OfflineUser
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Fullname { get; set; }
        public string? Position { get; set; }
        public string? Area { get; set; }
        public string? Image { get; set; }
        public string? LocalPath { get; set; }
    }
}
