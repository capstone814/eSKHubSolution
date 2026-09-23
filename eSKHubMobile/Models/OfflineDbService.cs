using SQLite;
using eSKHubMobile.Models;

namespace eSKHubMobile.Services
{
    public class OfflineDbService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public OfflineDbService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "eskhub_offline.db3");
        }

        private async Task Init()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath);
            await _database.CreateTableAsync<OfflineUser>();

            // Migration: Version 2 - Ensure RemoteId column exists and is populated.
            // A simple ALTER TABLE might not be enough if the mapping is cached or messy.
            // We'll force a recreation once to ensure clean state.
            if (!Preferences.Get("AnnouncementsTableReset_V2", false))
            {
                await _database.DropTableAsync<OfflineAnnouncement>();
                await _database.CreateTableAsync<OfflineAnnouncement>();
                Preferences.Set("AnnouncementsTableReset_V2", true);
            }
            else
            {
                await _database.CreateTableAsync<OfflineAnnouncement>();
            }

            // Migration: Version 3 - Reset tables to include Audience and TargetBarangay columns
            if (!Preferences.Get("AudienceFilteringReset_V3", false))
            {
                await _database.DropTableAsync<OfflineAnnouncement>();
                await _database.DropTableAsync<OfflineEvent>();
                await _database.CreateTableAsync<OfflineAnnouncement>();
                await _database.CreateTableAsync<OfflineEvent>();
                Preferences.Set("AudienceFilteringReset_V3", true);
            }
            else
            {
                await _database.CreateTableAsync<OfflineEvent>();
            }
            await _database.CreateTableAsync<OfflineNewsArticle>();
            await _database.CreateTableAsync<OfflineProgramProject>();
            await _database.CreateTableAsync<OfflinePoll>();
            await _database.CreateTableAsync<OfflinePollAnswer>();
            await _database.CreateTableAsync<OfflineDeletedPoll>();
            await _database.CreateTableAsync<OfflineFeedback>();
        }

        // Generic Methods
        public async Task<List<T>> GetItemsAsync<T>() where T : new()
        {
            await Init();
            return await _database!.Table<T>().ToListAsync();
        }

        public async Task<int> SaveItemsAsync<T>(IEnumerable<T> items) where T : new()
        {
            await Init();
            // Use a transaction for bulk operations to prevent UI freezing and improve speed
            await _database!.RunInTransactionAsync(conn =>
            {
                conn.DeleteAll<T>();
                conn.InsertAll(items);
            });
            return items.Count();
        }

        public async Task<int> SaveItemAsync<T>(T item) where T : new()
        {
            await Init();
            return await _database!.InsertOrReplaceAsync(item);
        }

        public async Task<int> DeleteAllItemsAsync<T>() where T : new()
        {
            await Init();
            return await _database!.DeleteAllAsync<T>();
        }

        // User methods
        public async Task SaveUserAsync(OfflineUser user)
        {
            await Init();
            await _database!.InsertOrReplaceAsync(user);
        }

        public async Task<OfflineUser?> GetUserAsync(string username)
        {
            await Init();
            return await _database!.Table<OfflineUser>()
                .Where(u => u.Username == username)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ValidateOfflineLoginAsync(string username, string hashedPassword)
        {
            await Init();
            var user = await _database!.Table<OfflineUser>()
                .Where(u => u.Username == username && u.Password == hashedPassword)
                .FirstOrDefaultAsync();
            return user != null;
        }

        // Announcement methods (kept for compatibility or specialized sorting)
        public async Task<List<OfflineAnnouncement>> GetAnnouncementsAsync()
        {
            await Init();
            return await _database!.Table<OfflineAnnouncement>()
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.DateCreated)
                .ToListAsync();
        }

        public async Task ClearAllDataAsync()
        {
            await Init();
            await _database!.DeleteAllAsync<OfflineUser>();
            await _database.DeleteAllAsync<OfflineAnnouncement>();
            await _database.DeleteAllAsync<OfflineEvent>();
            await _database.DeleteAllAsync<OfflineNewsArticle>();
            await _database.DeleteAllAsync<OfflineProgramProject>();
            await _database.DeleteAllAsync<OfflinePoll>();
            await _database.DeleteAllAsync<OfflinePollAnswer>();
            await _database.DeleteAllAsync<OfflineDeletedPoll>();
            await _database.DeleteAllAsync<OfflineFeedback>();
        }
        public async Task<List<OfflineFeedback>> GetUnsyncedFeedbackAsync()
        {
            await Init();
            return await _database!.Table<OfflineFeedback>()
                .Where(f => !f.IsSynced)
                .ToListAsync();
        }

        public async Task SaveFeedbackAsync(OfflineFeedback feedback)
        {
            await Init();
            if (feedback.Id == 0)
            {
                await _database!.InsertAsync(feedback);
            }
            else
            {
                await _database!.UpdateAsync(feedback);
            }
        }
        public async Task<List<OfflineFeedback>> GetUserFeedbackAsync(string email)
        {
            await Init();
            return await _database!.Table<OfflineFeedback>()
                .Where(f => f.Email == email)
                .OrderByDescending(f => f.DateSubmitted)
                .ToListAsync();
        }
        public async Task DeleteSyncedFeedbackAsync(string email)
        {
            await Init();
            var syncedItems = await _database!.Table<OfflineFeedback>()
                .Where(f => f.IsSynced && f.Email == email)
                .ToListAsync();

            foreach (var item in syncedItems)
            {
                await _database.DeleteAsync(item);
            }
        }

        // Poll methods
        public async Task<List<OfflinePollAnswer>> GetUnsyncedPollAnswersAsync()
        {
            await Init();
            return await _database!.Table<OfflinePollAnswer>()
                .Where(a => !a.IsSynced)
                .ToListAsync();
        }

        public async Task<OfflinePollAnswer?> GetUserPollAnswerAsync(int pollId, string username)
        {
            await Init();
            return await _database!.Table<OfflinePollAnswer>()
                .Where(a => a.RemotePollId == pollId && a.Username == username)
                .FirstOrDefaultAsync();
        }

        // Deleted Poll methods
        public async Task<List<OfflineDeletedPoll>> GetUnsyncedDeletedPollsAsync()
        {
            await Init();
            return await _database!.Table<OfflineDeletedPoll>()
                .Where(p => !p.IsSynced)
                .ToListAsync();
        }

        public async Task SaveDeletedPollAsync(OfflineDeletedPoll deletedPoll)
        {
            await Init();
            await _database!.InsertOrReplaceAsync(deletedPoll);
        }
    }
}