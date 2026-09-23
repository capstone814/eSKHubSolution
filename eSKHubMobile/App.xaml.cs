using eSKHubMobile.Services;

namespace eSKHubMobile
{
    public partial class App : Application
    {
        private readonly IAppNotificationService _notificationService;

        public App(IAppNotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage(_notificationService)) { Title = "eSKHubMobile" };
        }
    }
}
