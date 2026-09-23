using System.Threading.Tasks;
using eSKHubMobile.Models;

namespace eSKHubMobile.Services
{
    public interface IAppNotificationService
    {
        Task<bool> RequestPermissionsAsync();
        Task<bool> AreNotificationsEnabledAsync();
        void OpenAppSettings();
        Task ShowNotificationAsync(string title, string message, int notificationId = 101);
        Task ScheduleEventReminders(OfflineEvent ev);
        void CancelEventReminders(int eventId);
        Task ShowSyncSuccessNotificationAsync(int userCount, int announcementCount);
        Task ShowSyncErrorNotificationAsync(string errorMessage);
    }
}
