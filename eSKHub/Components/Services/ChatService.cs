using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using eSKHub.Components.Model;
using eSKHub.Model;

namespace eSKHub.Components.Services
{
    public class ChatService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ChatService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task SendMessageAsync(string sender, string receiver, string message)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var newMessage = new ChatMessage
            {
                SenderUsername = sender,
                ReceiverUsername = receiver,
                Message = message,
                Timestamp = DateTime.Now
            };
            db.ChatMessages.Add(newMessage);
            await db.SaveChangesAsync();
        }

        public async Task<List<ChatMessage>> GetConversationAsync(string user1, string user2)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.ChatMessages
                .Where(m => (m.SenderUsername == user1 && m.ReceiverUsername == user2) ||
                            (m.SenderUsername == user2 && m.ReceiverUsername == user1))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<List<User>> GetChatableUsersAsync(string currentUser)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            // Fetch users with roles Admin or Council, excluding the current user
            return await db.Users
                .Where(u => u.Username != currentUser && (u.Role == "Admin" || u.Role == "Council"))
                .Select(u => new User
                {
                    Id = u.Id,
                    Username = u.Username,
                    Fullname = u.Fullname,
                    Role = u.Role,
                    Position = u.Position,
                    Area = u.Area,
                    ImageData = u.ImageData,
                    ImageMimeType = u.ImageMimeType
                })
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(string sender, string receiver)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var unreadMessages = await db.ChatMessages
                .Where(m => m.SenderUsername == sender && m.ReceiverUsername == receiver && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.Now;
            }
            await db.SaveChangesAsync();
        }

        public async Task<int> GetTotalUnreadCountAsync(string currentUser)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.ChatMessages
                .Where(m => m.ReceiverUsername == currentUser && !m.IsRead)
                .CountAsync();
        }

        public async Task<Dictionary<string, int>> GetUnreadCountsPerUserAsync(string currentUser)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.ChatMessages
                .Where(m => m.ReceiverUsername == currentUser && !m.IsRead)
                .GroupBy(m => m.SenderUsername)
                .Select(g => new { Username = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Username, x => x.Count);
        }

        public async Task ClearConversationAsync(string user1, string user2)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var messages = await db.ChatMessages
                .Where(m => (m.SenderUsername == user1 && m.ReceiverUsername == user2) ||
                            (m.SenderUsername == user2 && m.ReceiverUsername == user1))
                .ToListAsync();

            db.ChatMessages.RemoveRange(messages);
            await db.SaveChangesAsync();
        }

        public async Task<Dictionary<string, string>> GetLastMessagesAsync(string currentUser)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var result = await db.ChatMessages
                .Where(m => m.SenderUsername == currentUser || m.ReceiverUsername == currentUser)
                .GroupBy(m => m.SenderUsername == currentUser ? m.ReceiverUsername : m.SenderUsername)
                .Select(g => new
                {
                    Partner = g.Key,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.Partner, x => x.LastMessage!.Message);

            return result;
        }
    }
}
