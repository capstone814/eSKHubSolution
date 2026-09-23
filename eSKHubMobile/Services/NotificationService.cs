using System.Threading.Tasks;
using Plugin.LocalNotification;
#if ANDROID
using Plugin.LocalNotification.AndroidOption;
using Android.App;
using Android.Content;
using Android.OS;
#endif
using eSKHubMobile.Models;

namespace eSKHubMobile.Services
{
    public class NotificationService : IAppNotificationService
    {
        private const string REMINDER_CHANNEL_ID = "event_reminders";

        public NotificationService()
        {
            InitializeChannels();
        }

        private void InitializeChannels()
        {
#if ANDROID
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channelId = REMINDER_CHANNEL_ID;
                var channelName = "Event Reminders";
                var channelDescription = "Critical reminders for upcoming events and updates";
                var importance = NotificationImportance.Max;

                var channel = new NotificationChannel(channelId, channelName, importance)
                {
                    Description = channelDescription,
                    LockscreenVisibility = NotificationVisibility.Public
                };
                channel.EnableVibration(true);
                channel.SetShowBadge(true);

                var notificationManager = (NotificationManager)Android.App.Application.Context.GetSystemService(Context.NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
                Console.WriteLine($"[NotificationService] ✓ Native Android Channel '{REMINDER_CHANNEL_ID}' registered.");
            }
#endif
        }

        public async Task<bool> AreNotificationsEnabledAsync()
        {
            return await LocalNotificationCenter.Current.AreNotificationsEnabled();
        }

        public void OpenAppSettings()
        {
            Microsoft.Maui.ApplicationModel.AppInfo.Current.ShowSettingsUI();
        }

        public async Task<bool> RequestPermissionsAsync()
        {
            if (await AreNotificationsEnabledAsync())
            {
                return true;
            }

            return await LocalNotificationCenter.Current.RequestNotificationPermission();
        }

        public async Task ShowNotificationAsync(string title, string message, int notificationId = 101)
        {
            try
            {
                // Ensure permissions are granted before trying to show
                if (!await RequestPermissionsAsync())
                {
                    Console.WriteLine("[NotificationService] ⚠ Cannot show notification: Permissions denied.");
                    return;
                }

                var request = new NotificationRequest
                {
                    NotificationId = notificationId,
                    Title = title,
                    Description = message,
                    BadgeNumber = 1,
                    Schedule =
                    {
                        NotifyTime = DateTime.Now
                    },
#if ANDROID
                    Android =
                    {
                        Priority = AndroidPriority.High,
                        VisibilityType = AndroidVisibilityType.Public,
                        ChannelId = REMINDER_CHANNEL_ID
                    }
#endif
                };

                await LocalNotificationCenter.Current.Show(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] ❌ Error showing notification: {ex.Message}");
            }
        }

        public async Task ScheduleEventReminders(OfflineEvent ev)
        {
            if (ev.RemoteId <= 0) return;

            // Cancel any existing reminders for this event first
            CancelEventReminders(ev.RemoteId);

            var eventTime = ev.EventDate.Date;
            if (ev.StartTime.HasValue)
            {
                eventTime = eventTime.Add(ev.StartTime.Value);
            }

            // 24 Hour Reminder
            var time24h = eventTime.AddHours(-24);
            if (time24h > DateTime.Now)
            {
                await ScheduleNotificationAsync(
                    ev.RemoteId * 10 + 1,
                    "Upcoming Event Tomorrow",
                    $"Reminder: '{ev.Title}' starts at {eventTime:hh:mm tt} tomorrow at {ev.Location}.",
                    time24h);
            }

            // 6 Hour Reminder
            var time6h = eventTime.AddHours(-6);
            if (time6h > DateTime.Now)
            {
                await ScheduleNotificationAsync(
                    ev.RemoteId * 10 + 2,
                    "Upcoming Event Today",
                    $"Reminder: '{ev.Title}' starts in 6 hours ({eventTime:hh:mm tt}) at {ev.Location}.",
                    time6h);
            }

            // 15 Minute Reminder
            var time15m = eventTime.AddMinutes(-15);
            if (time15m > DateTime.Now)
            {
                await ScheduleNotificationAsync(
                    ev.RemoteId * 10 + 3,
                    "Event Starting Soon",
                    $"Reminder: '{ev.Title}' starts in 15 minutes at {ev.Location}.",
                    time15m);
            }

            // Starting Now Reminder
            if (eventTime > DateTime.Now)
            {
                await ScheduleNotificationAsync(
                    ev.RemoteId * 10 + 4,
                    "Event Starting Now",
                    $"The event '{ev.Title}' is starting now at {ev.Location}.",
                    eventTime);
            }
        }

        public void CancelEventReminders(int eventId)
        {
            LocalNotificationCenter.Current.Cancel(new[] {
                eventId * 10 + 1, // 24h
                eventId * 10 + 2, // 6h
                eventId * 10 + 3, // 15m
                eventId * 10 + 4  // Now
            });
        }

        private async Task ScheduleNotificationAsync(int id, string title, string message, DateTime notifyTime)
        {
            try
            {
                // Ensure permissions are granted before scheduling
                if (!await RequestPermissionsAsync())
                {
                    Console.WriteLine($"[NotificationService] ⚠ Cannot schedule reminder {id}: Permissions denied.");
                    return;
                }

                var request = new NotificationRequest
                {
                    NotificationId = id,
                    Title = title,
                    Description = message,
                    BadgeNumber = 1,
                    Schedule =
                    {
                        NotifyTime = notifyTime,
                        RepeatType = NotificationRepeat.No
                    },
#if ANDROID
                    Android =
                    {
                        Priority = AndroidPriority.High,
                        VisibilityType = AndroidVisibilityType.Public,
                        ChannelId = REMINDER_CHANNEL_ID
                    }
#endif
                };

                await LocalNotificationCenter.Current.Show(request);
                Console.WriteLine($"[NotificationService] ✅ Reminder {id} scheduled for {notifyTime:yyyy-MM-dd HH:mm}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] ❌ Error scheduling reminder: {ex.Message}");
            }
        }

        public async Task ShowSyncSuccessNotificationAsync(int userCount, int announcementCount)
        {
            string message = $"Successfully synced {userCount} users and {announcementCount} announcements.";
            await ShowNotificationAsync("Sync Complete", message, 102);
        }

        public async Task ShowSyncErrorNotificationAsync(string errorMessage)
        {
            await ShowNotificationAsync("Sync Failed", errorMessage, 103);
        }
    }
}
