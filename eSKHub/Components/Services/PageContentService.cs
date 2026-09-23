using eSKHub.Components.Model;
using eSKHub.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eSKHub.Components.Services
{
    public class PageContentService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public PageContentService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<Dictionary<string, string>?> GetContactUsContentAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var content = await db.PageContents
                .FirstOrDefaultAsync(p => p.PageName == "ContactUs");

            if (content == null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(content.Content);
            }
            catch
            {
                return null;
            }
        }

        public async Task<Dictionary<string, string>?> GetPrivacyPolicyContentAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var content = await db.PageContents
                .FirstOrDefaultAsync(p => p.PageName == "PrivacyPolicy");

            if (content == null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(content.Content);
            }
            catch
            {
                return null;
            }
        }
    }
}