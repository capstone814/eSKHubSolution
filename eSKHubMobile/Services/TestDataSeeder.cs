using eSKHubMobile.Services;
using eSKHubMobile.Models;
using System.Security.Cryptography;
using System.Text;

namespace eSKHubMobile.Services
{
    public class TestDataSeeder
    {
        private readonly OfflineDbService _offlineDb;

        public TestDataSeeder(OfflineDbService offlineDb)
        {
            _offlineDb = offlineDb;
        }

        public async Task SeedTestDataAsync()
        {
            try
            {
                Console.WriteLine("[TestDataSeeder] Starting test data seed...");

                // Check if we already have data
                var existingUser = await _offlineDb.GetUserAsync("testcouncil");
                if (existingUser != null)
                {
                    Console.WriteLine("[TestDataSeeder] Test data already exists. Skipping seed.");
                    Console.WriteLine("[TestDataSeeder] Test credentials - Username: testcouncil, Password: password123");
                    return;
                }

                Console.WriteLine("[TestDataSeeder] Creating test user...");

                // Hash the password properly
                var password = "password123";
                var hashedPassword = HashPassword(password);
                Console.WriteLine($"[TestDataSeeder] Generated hash: {hashedPassword}");

                // Seed test user
                var testUser = new OfflineUser
                {
                    Id = 1,
                    Username = "testcouncil",
                    Password = hashedPassword,
                    Email = "council@eskhub.com",
                    Role = "Council",
                    Image = null
                };

                await _offlineDb.SaveUserAsync(testUser);
                Console.WriteLine("[TestDataSeeder] Test user saved successfully");

                // Verify the user was saved
                var savedUser = await _offlineDb.GetUserAsync("testcouncil");
                if (savedUser != null)
                {
                    Console.WriteLine($"[TestDataSeeder] Verification: User exists with role: {savedUser.Role}");
                }
                else
                {
                    Console.WriteLine("[TestDataSeeder] ERROR: User was not saved properly!");
                }

                // Seed test announcements
                Console.WriteLine("[TestDataSeeder] Creating test announcements...");
                var testAnnouncements = new List<OfflineAnnouncement>
                {
                    new OfflineAnnouncement
                    {
                        Id = 1,
                        Subject = "Welcome to eSKHub Mobile",
                        Body = "This is your mobile portal for accessing council announcements and updates. You can view announcements even when offline!",
                        Author = "Admin",
                        Priority = 5,
                        DateCreated = DateTime.Now.AddDays(-5)
                    },
                    new OfflineAnnouncement
                    {
                        Id = 2,
                        Subject = "Upcoming Council Meeting",
                        Body = "The next council meeting is scheduled for next Monday at 2:00 PM. Please review the agenda in advance.",
                        Author = "Secretary",
                        Priority = 4,
                        DateCreated = DateTime.Now.AddDays(-3)
                    },
                    new OfflineAnnouncement
                    {
                        Id = 3,
                        Subject = "Budget Review Notice",
                        Body = "All council members are required to review the Q4 budget report before the next meeting. Documents are available in the shared drive.",
                        Author = "Treasurer",
                        Priority = 3,
                        DateCreated = DateTime.Now.AddDays(-2)
                    },
                    new OfflineAnnouncement
                    {
                        Id = 4,
                        Subject = "Community Outreach Program",
                        Body = "We are launching a new community outreach initiative. Volunteers are needed for the weekend activities.",
                        Author = "Community Relations",
                        Priority = 2,
                        DateCreated = DateTime.Now.AddDays(-1)
                    },
                    new OfflineAnnouncement
                    {
                        Id = 5,
                        Subject = "System Maintenance Notice",
                        Body = "The online portal will undergo scheduled maintenance this Saturday from 12:00 AM to 4:00 AM. Mobile app will continue to work offline.",
                        Author = "IT Department",
                        Priority = 1,
                        DateCreated = DateTime.Now
                    }
                };

                await _offlineDb.SaveItemsAsync(testAnnouncements);
                Console.WriteLine($"[TestDataSeeder] {testAnnouncements.Count} announcements saved successfully");

                // Verify announcements
                var savedAnnouncements = await _offlineDb.GetAnnouncementsAsync();
                Console.WriteLine($"[TestDataSeeder] Verification: {savedAnnouncements.Count} announcements in database");

                Console.WriteLine("═══════════════════════════════════════════════════════");
                Console.WriteLine("TEST DATA SEEDED SUCCESSFULLY!");
                Console.WriteLine("═══════════════════════════════════════════════════════");
                Console.WriteLine("Test Credentials:");
                Console.WriteLine("  Username: testcouncil");
                Console.WriteLine("  Password: password123");
                Console.WriteLine("═══════════════════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TestDataSeeder] ERROR seeding test data: {ex.Message}");
                Console.WriteLine($"[TestDataSeeder] Stack trace: {ex.StackTrace}");
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}