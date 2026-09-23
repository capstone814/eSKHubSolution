using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using eSKHub.Components.Model;

namespace eSKHub.Model
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AppDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                var connStr = _configuration.GetConnectionString("DatabaseConnectionString");
                if (!string.IsNullOrEmpty(connStr) && (connStr.StartsWith("postgres", StringComparison.OrdinalIgnoreCase) || connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase)))
                {
                    var npgsqlConnStr = ConvertPostgresUriToConnectionString(connStr);
                    options.UseNpgsql(npgsqlConnStr);
                }
                else
                {
                    options.UseSqlServer(connStr);
                }
            }
        }

        private static string ConvertPostgresUriToConnectionString(string connStr)
        {
            if (!connStr.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
                return connStr;

            try
            {
                var uri = new Uri(connStr);
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo.Length > 0 ? userInfo[0] : "";
                var password = userInfo.Length > 1 ? userInfo[1] : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');

                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
            }
            catch
            {
                return connStr;
            }
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<NewsArticle> NewsArticles { get; set; }
        public DbSet<ProgramProject> ProgramsProjects { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Youth> Youths { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<eSKHub.Components.Model.TransparencyDocument> TransparencyDocuments { get; set; }
        public DbSet<PageContent> PageContents { get; set; }
        public DbSet<ProgramParticipation> ProgramParticipations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
    }
}