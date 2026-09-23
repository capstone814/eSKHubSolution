using Android.Content;
using AndroidX.Work;

namespace eSKHubMobile.Platforms.Android
{
    /// <summary>
    /// Receives BOOT_COMPLETED and re-schedules the WorkManager periodic sync
    /// so it survives device reboots.
    /// </summary>
    [BroadcastReceiver(
        Enabled = true,
        Exported = true,
        DirectBootAware = false)]
    public class SyncBootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != Intent.ActionBootCompleted &&
                intent?.Action != "android.intent.action.QUICKBOOT_POWERON")
                return;

            Console.WriteLine("[SyncBootReceiver] 📱 Device rebooted – rescheduling WorkManager sync.");

            if (context == null) return;

            SyncWorkerScheduler.Schedule(context);
        }
    }

    /// <summary>
    /// Shared helper to register (or replace) the periodic WorkManager request.
    /// Called from both SyncBootReceiver and MainApplication.
    /// </summary>
    public static class SyncWorkerScheduler
    {
        // Android WorkManager minimum periodic interval is 15 minutes.
        // Set to 60 minutes to be battery-friendly.
        private const long REPEAT_INTERVAL_MINUTES = 60L;

        public static void Schedule(Context context)
        {
            try
            {
                // Build constraints — only sync when connected to the internet
                var constraints = new AndroidX.Work.Constraints.Builder()
                    .SetRequiredNetworkType(NetworkType.Connected)
                    .Build();

                // Build periodic work request
                var workRequest = new PeriodicWorkRequest.Builder(
                        typeof(SyncWorker),
                        REPEAT_INTERVAL_MINUTES,
                        Java.Util.Concurrent.TimeUnit.Minutes!)
                    .SetConstraints(constraints)
                    .AddTag(SyncWorker.WORK_NAME)
                    .Build();

                // Enqueue with KEEP policy so existing schedule is not replaced
                // on every app open — only if not already enqueued.
                WorkManager
                    .GetInstance(context)
                    .EnqueueUniquePeriodicWork(
                        SyncWorker.WORK_NAME,
                        ExistingPeriodicWorkPolicy.Keep,
                        workRequest);

                Console.WriteLine($"[SyncWorkerScheduler] ✓ Scheduled '{SyncWorker.WORK_NAME}' every {REPEAT_INTERVAL_MINUTES} min.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncWorkerScheduler] ❌ Schedule error: {ex.Message}");
            }
        }
    }
}
