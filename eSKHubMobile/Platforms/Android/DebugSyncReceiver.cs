using Android.Content;
using AndroidX.Work;

namespace eSKHubMobile.Platforms.Android
{
    /// <summary>
    /// DEBUG ONLY — Broadcast receiver that triggers a one-time background sync immediately.
    /// Trigger via ADB:
    ///   adb shell am broadcast -a com.eskhub770.eskhubmobile_770.DEBUG_SYNC -p com.eskhub770.eskhubmobile_770
    /// </summary>
    [BroadcastReceiver(
        Enabled = true,
        Exported = true,
        Name = "com.eskhub770.eskhubmobile_770.DebugSyncReceiver")]
    [global::Android.App.IntentFilter(new[] { "com.eskhub770.eskhubmobile_770.DEBUG_SYNC" })]
    public class DebugSyncReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            global::Android.Util.Log.Info("eSKHubSync", "DebugSyncReceiver OnReceive triggered!");
            if (context == null)
            {
                global::Android.Util.Log.Warn("eSKHubSync", "DebugSyncReceiver: Context is null.");
                return;
            }
            if (intent?.Action != "com.eskhub770.eskhubmobile_770.DEBUG_SYNC")
            {
                global::Android.Util.Log.Warn("eSKHubSync", $"DebugSyncReceiver: Unexpected action: {intent?.Action}");
                return;
            }

            global::Android.Util.Log.Info("eSKHubSync", "🐛 ADB debug sync triggered — enqueuing one-time SyncWorker.");

            try
            {
                var constraints = new Constraints.Builder()
                    .SetRequiredNetworkType(NetworkType.Connected)
                    .Build();

                var oneTimeRequest = new OneTimeWorkRequest.Builder(typeof(SyncWorker))
                    .SetConstraints(constraints)
                    .AddTag("debug_sync")
                    .Build();

                WorkManager
                    .GetInstance(context)
                    .Enqueue(oneTimeRequest);

                global::Android.Util.Log.Info("eSKHubSync", "✓ One-time SyncWorker enqueued successfully.");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("eSKHubSync", $"❌ Error enqueuing SyncWorker: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
