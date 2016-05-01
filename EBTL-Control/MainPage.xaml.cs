using BackgroundTasks.Helpers;
using EBTL;
using EBTL_Control.Model;
using EBTL_Control.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Maps;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Documents;   // For multiple lines of text.
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EBTL_Control
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Attributes

        public static MainPage _MainPage;

        private CancellationTokenSource _cts;
        private uint _UpdateDelta = 7200; // 2 Hours.

        private Geoposition _HospitalGeoLocation;
        private Geopoint _HospitalGeoPoint;
        private PointOfInterestsManager _POIManager;

        private string NotificationPayload;

        // Should be bound to MapItems.ItemsSource, but it cannot.
        public ObservableCollection<PointOfInterest> _Donors { get; private set; }

        private static readonly string BACKGROUND_ENTRY_POINT = typeof(BackgroundTasks.ToastActivationTypeBackgroundClosedTask).FullName;

        #endregion Attributes

        #region ENUMS

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        private enum PayloadType
        {
            Pop,
            BackgroundAppClosed,
            Protocol
        }

        #endregion ENUMS

        public MainPage()
        {
            this.InitializeComponent();

            _MainPage = this;

            InitializeLocationService();
            SetupNotificationContent(PayloadType.BackgroundAppClosed, null);

            InitializeNotificationsHub();

            InitializeBackgroundCommunication();

            MainMap.Loaded += MainMapLoaded;
            MainMap.MapTapped += MainMap_MapTapped;
        }

        #region AzureNotificationHub

        /// <summary>
        /// https://azure.microsoft.com/en-us/documentation/articles/notification-hubs-windows-store-dotnet-get-started/#send-notifications
        /// </summary>
        private void InitializeNotificationsHub()
        {
            //var channel = await Windows.Networking.PushNotifications.PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

            //var hub = new NotificationHub("<hub name>", "<connection string with listen access>");
            //var result = await hub.RegisterNativeAsync(channel.Uri);

            //// Displays the registration ID so you know it was successful
            //if (result.RegistrationId != null)
            //{
            //    var dialog = new MessageDialog("Registration successful: " + result.RegistrationId);
            //    dialog.Commands.Add(new UICommand("OK"));
            //    await dialog.ShowAsync();
            //}
        }

        #endregion AzureNotificationHub

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

        #region NotificationService

        private void LaunchNotification(PointOfInterest _FoundDonor)
        {
            // TODO ... App to App service to send data to EBTL Client application.
            ToastNotificationManager.History.Clear();

            // https://blogs.msdn.microsoft.com/tiles_and_toasts/2015/07/08/quickstart-sending-a-local-toast-notification-and-handling-activations-from-it-windows-10/
            // http://stackoverflow.com/questions/36068229/uwp-how-do-a-process-buttons-displayed-in-a-toast-that-are-launched-from-a-backg
            // http://fr.slideshare.net/shahedC3000/deeper-into-windows-10-development
            // https://blogs.msdn.microsoft.com/tiles_and_toasts/2015/07/02/adaptive-and-interactive-toast-notifications-for-windows-10/

            // This is the correct one.
            string BingMapsURI = @"bingmaps:?rtp=~pos." + _FoundDonor.Location.Position.Latitude.ToString() + "_" + _FoundDonor.Location.Position.Longitude.ToString();
            // Also correct.
            string DriveToURI = @"ms-drive-to:?destination.latitude=" + _FoundDonor.Location.Position.Latitude.ToString() + "&destination.longitude=" + _FoundDonor.Location.Position.Longitude.ToString();

            //SetupNotificationContent(PayloadType.BackgroundAppClosed, DriveToURI);

            //Error(NotificationPayload);

            ToastHelper.PopToast("Path to hospital", DriveToURI);
            LaunchMaps(DriveToURI);
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
            else
            {
                var uri = result["message"] as string;
                Error(uri);
                // LaunchMaps(uri);
            }
        }

        private async void LaunchMaps(string DriveToURI)
        {
            // Center on New York City
            var uriNewYork = new Uri(DriveToURI);

            // Launch the Windows Maps app
            var launcherOptions = new Windows.System.LauncherOptions();
            launcherOptions.TargetApplicationPackageFamilyName = "Microsoft.WindowsMaps_8wekyb3d8bbwe";
            var success = await Windows.System.Launcher.LaunchUriAsync(uriNewYork, launcherOptions);
            //if (success)
            //{
            //    _MainPage.NotifyUser(success.ToString(), NotifyType.StatusMessage);
            //}
        }

        private void SetupNotificationContent(PayloadType _Type, string uri)
        {
            switch (_Type)
            {
                case PayloadType.Pop:
                    // Pop notifications
                    NotificationPayload =
                        $@"
                <toast activationType='foreground' launch='args'>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text>Action - text</text>
                            <text>Make sure left button on the toast has the text ""ok"" on it, and the right button has the text ""cancel"" on it.</text>
                        </binding>
                    </visual>
                    <actions>

                        <action
                            content='ok'
                            activationType='foreground'
                            arguments='check'/>

                        <action
                            content='cancel'
                            activationType='foreground'
                            arguments='cancel'/>

                    </actions>
                </toast>";
                    break;

                case PayloadType.BackgroundAppClosed:

                    if (uri != null)
                    {
                        NotificationPayload =
                $@"
                <toast activationType='background' launch='args'>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text>Background with App Closed</text>
                            <text>Ensure the app is closed. Make sure ""Windows 10"" is in the first text box. Press ""submit"".</text>
                        </binding>
                    </visual>
                    <actions>

                        <input
                            id = 'message'
                            type = 'text'
                            title = 'Message'
                            placeHolderContent = 'Enter ""Windows 10""'
                            defaultInput = '" + uri + $@"' />

                        <action activationType = 'background'
                                arguments = 'quickReply'
                                content = 'submit' />

                        <action activationType = 'background'
                                arguments = 'cancel'
                                content = 'cancel' />

                    </actions>
                </toast>";
                    }
                    else
                    {
                        NotificationPayload =
                $@"
                <toast activationType='background' launch='args'>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text>Background with App Closed</text>
                            <text>Ensure the app is closed. Make sure ""Windows 10"" is in the first text box. Press ""submit"".</text>
                        </binding>
                    </visual>
                    <actions>

                        <input
                            id = 'message'
                            type = 'text'
                            title = 'Message'
                            placeHolderContent = 'Enter ""Windows 10""'
                            defaultInput = 'Windows 10' />

                        <action activationType = 'background'
                                arguments = 'quickReply'
                                content = 'submit' />

                        <action activationType = 'background'
                                arguments = 'cancel'
                                content = 'cancel' />

                    </actions>
                </toast>";
                    }

                    break;

                case PayloadType.Protocol:
                    break;

                default:
                    break;
            }
        }

        #endregion NotificationService

        #region MapFunctions

        private void MainMapLoaded(object sender, RoutedEventArgs e)
        {
            MainMap.Center = _HospitalGeoPoint;

            MainMap.ZoomLevel = 12;
            _POIManager = new PointOfInterestsManager();

            // Create the databinding here.

            // Make a new source, to grab a new timestamp
            _Donors = new ObservableCollection<PointOfInterest>();
        }

        private void MainMap_MapTapped(Windows.UI.Xaml.Controls.Maps.MapControl sender, Windows.UI.Xaml.Controls.Maps.MapInputEventArgs args)
        {
            var tappedGeoPosition = args.Location.Position;

            //string status = "MapTapped at \nLatitude:" + tappedGeoPosition.Latitude + "\nLongitude: " + tappedGeoPosition.Longitude;
            //_MainPage.NotifyUser(status, NotifyType.StatusMessage);

            var _GeoPoint = new Geopoint(tappedGeoPosition);
            Donor _TempDonor = new Donor("Temp " + _Donors.Count, "Test", "07855555524", "Address: Random Address", "AB+")
            {
                GeoPoint = _GeoPoint
            };

            AddDonorOnMap(_TempDonor);

            AddPOIOnMap(_GeoPoint, _TempDonor);

            //GetRouteAndDirections(tappedGeoPosition);
        }

        private void AddPOIOnMap(Geopoint _GeoPoint, Donor _TempDonor)
        {
            // Add a more beautiful pin.
            // Create a MapIcon.
            MapIcon mapIcon1 = new MapIcon();
            mapIcon1.Location = _GeoPoint;
            mapIcon1.NormalizedAnchorPoint = new Point(0.5, 1.0);

            mapIcon1.Title = _TempDonor.Surname;
            mapIcon1.ZIndex = 0;

            // Add the MapIcon to the map.
            MainMap.MapElements.Add(mapIcon1);
        }

        private void QueryDonors(string _Filter)
        {
            // This function must ask the database for all donors according to the filter defined.
        }

        private void AddDonorOnMap(Donor _Donor)
        {
            _Donors.Add(new PointOfInterest(_Donor));

            //UpdateMapItemsControlItemsSource();
        }

        /// <summary>
        /// The stupid way to update the source of items to display within the map.
        /// One cannot bind the ItemsSource of the MapControl in another way.
        /// </summary>
        /// <see cref="http://msdn.microsoft.com/EN-US/library/dn792121(v=VS.10,d=hv.2).aspx"/>
        private void UpdateMapItemsControlItemsSource()
        {
            //MapItems.ItemsSource = null;// Apparently not needed now. What the f?
            MapItems.ItemsSource = _Donors;
        }

        /// Will use this to get the information.
        private void MapPoI_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var buttonSender = sender as Button;
            PointOfInterest poi = buttonSender.DataContext as PointOfInterest;
            var _poiLocation = "Lat: " + poi.Location.Position.Latitude + "\n" + poi.Location.Position.Longitude;
            _MainPage.NotifyUser("Donor Information: " + poi.DisplayName + ";\n" + poi.Address + ";\n" + _poiLocation, NotifyType.StatusMessage);
        }

        #endregion MapFunctions

        #region SearchFunctions

        private async void SearchClosestDonor(string BloodTypeRequired)
        {
            var _RequestDonors = new System.Collections.Generic.List<PointOfInterest>();

            foreach (var _PossibleDonor in _Donors)
            {
                if (_PossibleDonor.BloodType.Contains(BloodTypeRequired))
                {
                    _RequestDonors.Add(_PossibleDonor);
                }
            }

            double _MinDistance = 100; // Kilometers.
            double _MinType = 100; // Minutes.
            PointOfInterest _ClosesestDonor = null;
            foreach (var _Donor in _RequestDonors)
            {
                try
                {
                    MapRouteFinderResult result = await GetRouteAndDirections(_Donor.Location.Position);
                    if (result.Status == MapRouteFinderStatus.Success)
                    {
                        var _EstimatedTime = result.Route.EstimatedDuration.TotalMinutes;
                        var _Distance = (result.Route.LengthInMeters / 1000);

                        if (_Distance < _MinDistance)
                        {
                            if (_EstimatedTime < _MinType)
                            {
                                // This donor may be able to come in time.
                                _ClosesestDonor = _Donor;
                                _MinDistance = _Distance;
                                _MinType = _EstimatedTime;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            if (_ClosesestDonor != null)
            {
                _MainPage.NotifyUser("Closest donor:" + _ClosesestDonor.DisplayName + "\n" + _ClosesestDonor.Location + "\n" + _ClosesestDonor.EmergencyNumber, NotifyType.StatusMessage);
                LaunchNotification(_ClosesestDonor);
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SearchClosestDonor(bloodtype.Text);
        }

        #endregion SearchFunctions

        #region BingMapService

        /// <summary>
        /// Ask the Windows Maps service for the route and directions.
        /// </summary>
        /// <param name="startLocation"></param>
        /// <seealso cref="https://msdn.microsoft.com/en-us/library/windows/apps/xaml/dn631250.aspx"/>
        /// <returns></returns>
        private async Task<MapRouteFinderResult> GetRouteAndDirections(BasicGeoposition startLocation)
        {
            Geopoint startPoint = new Geopoint(startLocation);

            Geopoint endPoint = _HospitalGeoPoint;

            // Get the route between the points.
            MapRouteFinderResult routeResult =
                await MapRouteFinder.GetDrivingRouteAsync(
                startPoint,
                endPoint,
                MapRouteOptimization.Time,
                MapRouteRestrictions.None);

            //ShowRouteData(routeResult);

            return routeResult;
        }

        private void ShowRouteData(MapRouteFinderResult routeResult)
        {
            if (routeResult.Status == MapRouteFinderStatus.Success)
            {
                var _EstimatedTime = routeResult.Route.EstimatedDuration.TotalMinutes;
                var _Distance = (routeResult.Route.LengthInMeters / 1000);

                {
                    textBlock_RouteData.Text = "";

                    // Display summary info about the route.
                    textBlock_RouteData.Inlines.Add(new Run()
                    {
                        Text = "Total estimated time (minutes) = "
                            + _EstimatedTime.ToString()
                    });
                    textBlock_RouteData.Inlines.Add(new LineBreak());
                    textBlock_RouteData.Inlines.Add(new Run()
                    {
                        Text = "Total length (kilometers) = "
                            + _Distance.ToString()
                    });
                    textBlock_RouteData.Inlines.Add(new LineBreak());
                    textBlock_RouteData.Inlines.Add(new LineBreak());

                    // Display the directions.
                    textBlock_RouteData.Inlines.Add(new Run()
                    {
                        Text = "DIRECTIONS"
                    });
                    textBlock_RouteData.Inlines.Add(new LineBreak());

                    foreach (MapRouteLeg leg in routeResult.Route.Legs)
                    {
                        foreach (MapRouteManeuver maneuver in leg.Maneuvers)
                        {
                            textBlock_RouteData.Inlines.Add(new Run()
                            {
                                Text = maneuver.InstructionText
                            });
                            textBlock_RouteData.Inlines.Add(new LineBreak());
                        }
                    }
                }
            }
            else
            {
                textBlock_RouteData.Text =
                                    "A problem occurred: " + routeResult.Status.ToString();
            }
        }

        #endregion BingMapService

        #region GeoLocationService

        private async void InitializeLocationService()
        {
            _HospitalGeoPoint = new Geopoint(new BasicGeoposition()
            {
                //Geopoint for Seattle
                Latitude = 47.604,
                Longitude = -122.329
            });

            try
            {
                var accessStatus = await Geolocator.RequestAccessAsync();

                switch (accessStatus)
                {
                    case GeolocationAccessStatus.Unspecified:
                        // Inform the user that we will use his address for location but that his decision may cost a life.
                        // Battery before lives no?
                        InformUserOfUnspecifiedChoice();
                        break;

                    case GeolocationAccessStatus.Allowed:
                        //HideLocationDisablesInformation();

                        // Hide the information text block as the user allowed location services.
                        //textBlock_Information.Visibility = Visibility.Collapsed;
                        // Make a toast to say thank you.
                        //stackPanel_Setup.Visibility = Visibility.Visible;

                        // Get cancellation token
                        _cts = new CancellationTokenSource();
                        CancellationToken token = _cts.Token;

                        //_rootPage.NotifyUser("Waiting for update...", NotifyType.StatusMessage);

                        // If DesiredAccuracy or DesiredAccuracyInMeters are not set (or value is 0), DesiredAccuracy.Default is used.
                        Geolocator geolocator = new Geolocator
                        {
                            // Define period.
                            ReportInterval = _UpdateDelta
                        };

                        // Subscribe to the PositionChanged event to get location updates.
                        // We don't actually care about this.
                        //geolocator.PositionChanged += OnPositionChanged;

                        // Subscribe to StatusChanged event to get updates of location status changes.
                        // Don't care about this one either.
                        //geolocator.StatusChanged += OnStatusChanged;

                        //_rootPage.NotifyUser("Waiting for update...", NotifyType.StatusMessage);
                        //LocationDisabledMessage.Visibility = Visibility.Collapsed;

                        // Carry out the operation
                        Geoposition pos = await geolocator.GetGeopositionAsync().AsTask(token);

                        UpdateLocationData(pos);
                        // _rootPage.NotifyUser("Location updated.", NotifyType.StatusMessage);

                        break;

                    case GeolocationAccessStatus.Denied:
                        InformUserOfDeniedChoice();
                        break;

                    default:
                        break;
                }
            }
            catch (TaskCanceledException)
            {
                //throw;
            }
            catch (Exception)
            {
                //Nothing yet
            }
            finally
            {
                _cts = null;
            }

            //GetGeolocationButton.IsEnabled = true;
            //CancelGetGeolocationButton.IsEnabled = false;
        }

        private void InformUserOfUnspecifiedChoice()
        {
            //LocationDisabledMessage.Visibility = Visibility.Visible;
        }

        private void InformUserOfDeniedChoice()
        {
            // Inform user that this app requires location data in order to function.
            // If the device cannot be located, ask for an address and use bing map service.
            // Set the link to settings to visible.
            //LocationDisabledMessage.Visibility = Visibility.Visible;
        }

        private void UpdateLocationData(Geoposition pos)
        {
            if (pos != null)
            {
                MainMap.Center = _HospitalGeoPoint = new Geopoint(new BasicGeoposition()
                {
                    Latitude = pos.Coordinate.Latitude,
                    Longitude = pos.Coordinate.Longitude,
                    Altitude = pos.Coordinate.Point.Position.Altitude
                });

                _HospitalGeoLocation = pos;
            }
            else
            {
                // Dunno yet.
            }
        }

        async private void OnStatusChanged(Geolocator sender, StatusChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Show the location setting message only if status is disabled.
                //LocationDisabledMessage.Visibility = Visibility.Collapsed;

                switch (e.Status)
                {
                    case PositionStatus.Ready:
                        // Location platform is providing valid data.
                        //ScenarioOutput_Status.Text = "Ready";
                        //_rootPage.NotifyUser("Location platform is ready.", NotifyType.StatusMessage);
                        break;

                    case PositionStatus.Initializing:
                        // Location platform is attempting to acquire a fix.
                        //ScenarioOutput_Status.Text = "Initializing";
                        //_rootPage.NotifyUser("Location platform is attempting to obtain a position.", NotifyType.StatusMessage);
                        break;

                    case PositionStatus.NoData:
                        // Location platform could not obtain location data.
                        //ScenarioOutput_Status.Text = "No data";
                        //_rootPage.NotifyUser("Not able to determine the location.", NotifyType.ErrorMessage);
                        break;

                    case PositionStatus.Disabled:
                        // The permission to access location data is denied by the user or other policies.
                        //ScenarioOutput_Status.Text = "Disabled";
                        //_rootPage.NotifyUser("Access to location is denied.", NotifyType.ErrorMessage);

                        // Show message to the user to go to location settings.
                        //LocationDisabledMessage.Visibility = Visibility.Visible;

                        // Clear any cached location data.
                        UpdateLocationData(null);
                        break;

                    case PositionStatus.NotInitialized:
                        // The location platform is not initialized. This indicates that the application
                        // has not made a request for location data.
                        //ScenarioOutput_Status.Text = "Not initialized";
                        //_rootPage.NotifyUser("No request for location is made yet.", NotifyType.StatusMessage);
                        break;

                    case PositionStatus.NotAvailable:
                        // The location platform is not available on this version of the OS.
                        //ScenarioOutput_Status.Text = "Not available";
                        //_rootPage.NotifyUser("Location is not available on this version of the OS.", NotifyType.ErrorMessage);
                        break;

                    default:
                        //ScenarioOutput_Status.Text = "Unknown";
                        //_rootPage.NotifyUser(string.Empty, NotifyType.StatusMessage);
                        break;
                }
            });
        }

        async private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //_rootPage.NotifyUser("Location updated.", NotifyType.StatusMessage);
                UpdateLocationData(args.Position);
            });
        }

        #endregion GeoLocationService

        #region StatusNotifications

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

        #endregion StatusNotifications
    }
}