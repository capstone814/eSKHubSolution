using Microsoft.EntityFrameworkCore;
using eSKHubMobile.Models;
using Microsoft.Extensions.Configuration;

namespace eSKHubMobile.Data
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
            var connectionString = _configuration.GetConnectionString("DatabaseConnectionString");
            options.UseSqlServer(connectionString);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
    }
}