using Android.App;
using Android.Runtime;
using eSKHubMobile.Platforms.Android;

namespace eSKHubMobile
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();

            // Schedule the background sync WorkManager worker.
            // Uses ExistingPeriodicWorkPolicy.Keep so this is a no-op
            // if the worker was already scheduled from a previous run.
            SyncWorkerScheduler.Schedule(this);
        }
    }
}
