using BackgroundTasks.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EBTL
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ActivatedPage : Page
    {
        private static ActivatedPage _MainPage;

        public static StorageFolder localFolder { get; set; }
        public static ApplicationDataContainer localSettings { get; set; }

        private static readonly string BACKGROUND_ENTRY_POINT = typeof(BackgroundTasks.ToastActivationTypeBackgroundClosedTask).FullName;

        /// <summary>
        /// The page that gets called after the user has suscribed.
        /// </summary>
        public ActivatedPage()
        {
            this.InitializeComponent();

            _MainPage = this;
            Initialize();
            InitializeBackgroundCommunication();
        }

        #region BackgroundTasks

        private async void InitializeBackgroundCommunication()
        {
            // Register background task
            if (!await RegisterBackgroundTask())
            {
                await new MessageDialog("ERROR: Couldn't register background task.").ShowAsync();
                return;
            }
        }

        private async Task<bool> RegisterBackgroundTask()
        {
            // Unregister any previous exising background task
            UnregisterBackgroundTask();

            // Request access
            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();

            // If denied
            if (status != BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity && status != BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
                return false;

            // Construct the background task
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder()
            {
                Name = BACKGROUND_ENTRY_POINT,
                TaskEntryPoint = BACKGROUND_ENTRY_POINT
            };

            // Set trigger for Toast History Changed
            builder.SetTrigger(new ToastNotificationActionTrigger());

            // And register the background task
            BackgroundTaskRegistration registration = builder.Register();

            return true;
        }

        private static void UnregisterBackgroundTask()
        {
            var task = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(i => i.Name.Equals(BACKGROUND_ENTRY_POINT));

            if (task != null)
                task.Unregister(true);
        }

        #endregion BackgroundTasks

        private void Initialize()
        {
            // Listen to activation event
            (Application.Current as App).Activated = new EventHandler<IActivatedEventArgs>(App_Activated);
        }

        private async void App_Activated(object sender, IActivatedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, delegate
            {
                if (e.Kind != ActivationKind.ToastNotification)
                    return;

                var toastArgs = e as ToastNotificationActivatedEventArgs;
                if (toastArgs == null)
                {
                    Error("Activation args was not of type ToastNotificationActivatedEventArgs");
                    return;
                }

                string arguments = toastArgs.Argument;

                if (arguments == null || !arguments.Equals("quickReply"))
                {
                    Error($"Expected arguments to be 'quickReply' but was '{arguments}'. User input values...\n{ToastHelper.ToString(toastArgs.UserInput)}");
                    return;
                }

                // TODO This is where we can handle the notification input.
                validateStep1(toastArgs.UserInput);
                //switch (stepsControl.Step)
                //{
                //    case 1:
                //        validateStep1(toastArgs.UserInput);
                //        break;
                //}
            });
        }

        private async void Error(string message)
        {
            await new MessageDialog(message, "ERROR").ShowAsync();
        }

        private void validateStep1(ValueSet result)
        {
            if (result.Count != 1)
                Error("Expected 1 user input value, but there were " + result.Count);
            else if (!result.ContainsKey("message"))
                Error("Expected a user input value for 'message', but there was none.");
            else if (!(result["message"] as string).Equals("Windows 10"))
                Error("User input value for 'message' was not 'Windows 10'");
            else
            {
                //stepsControl.Step = int.MaxValue;
                PanelEmergency.Visibility = Visibility.Visible;
            }
        }

        #region TODO

        private void appBarButton_Settings_Click(object sender, RoutedEventArgs e)
        {
        }

        private void appBarButton_Yes_Click(object sender, RoutedEventArgs e)
        {
        }

        private void appBarButton_No_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion TODO

        /// <summary>
        /// Used to display messages to the user
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;

                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };
    }
}