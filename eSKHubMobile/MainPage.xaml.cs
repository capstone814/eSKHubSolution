using eSKHubMobile.Services;

namespace eSKHubMobile
{
    public partial class MainPage : ContentPage
    {
        public MainPage(IAppNotificationService notificationService)
        {
            InitializeComponent();

            // Request notification permissions on startup
            Task.Run(async () => await notificationService.RequestPermissionsAsync());
        }
    }
}
