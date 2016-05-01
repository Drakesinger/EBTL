﻿using BackgroundTasks.Helpers;
using EBTL;
using EBTL_Control.Model;
using EBTL_Control.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

using Windows.Services.Maps;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        public static MainPage _MainPage;

        private CancellationTokenSource _cts;
        private uint _UpdateDelta = 7200; // 2 Hours.

        private Geoposition _HospitalGeoLocation;
        private Geopoint _HospitalGeoPoint;
        private PointOfInterestsManager _POIManager;

        private string NotificationPayload;

        // Will be bound to MapItems.ItemsSource;
        public ObservableCollection<PointOfInterest> _Donors { get; private set; }

        //private List<PointOfInterest> _Donors;

        public MainPage()
        {
            this.InitializeComponent();

            _MainPage = this;

            InitializeLocationService();
            SetupNotificationContent();

            MainMap.Loaded += MainMapLoaded;
            MainMap.MapTapped += MainMap_MapTapped;
        }

        private void MainMap_MapTapped(Windows.UI.Xaml.Controls.Maps.MapControl sender, Windows.UI.Xaml.Controls.Maps.MapInputEventArgs args)
        {
            var tappedGeoPosition = args.Location.Position;
            //string status = "MapTapped at \nLatitude:" + tappedGeoPosition.Latitude + "\nLongitude: " + tappedGeoPosition.Longitude;
            //_MainPage.NotifyUser(status, NotifyType.StatusMessage);

            Donor _TempDonor = new Donor("Temp " + _Donors.Count, "Test", "07855555524", "Address: Random Address", "AB+")
            {
                GeoPoint = new Geopoint(new BasicGeoposition()
                {
                    Latitude = tappedGeoPosition.Latitude,
                    Longitude = tappedGeoPosition.Longitude
                })
            };

            AddDonorOnMap(_TempDonor);
            //GetRouteAndDirections(tappedGeoPosition);
        }

        private void QueryDonors(string _Filter)
        {
            // This function must ask the database for all donors according to the filter defined.
        }

        private void AddDonorOnMap(Donor _Donor)
        {
            _Donors.Add(new PointOfInterest(_Donor));

            UpdateMapItemsControlItemsSource();
        }

        /// <summary>
        /// The stupid way to update the source of items to display within the map.
        /// One cannot bind the ItemsSource of the MapControl in another way.
        /// </summary>
        /// <see cref="http://msdn.microsoft.com/EN-US/library/dn792121(v=VS.10,d=hv.2).aspx"/>
        private void UpdateMapItemsControlItemsSource()
        {
            MapItems.ItemsSource = null;
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
                _MainPage.NotifyUser("Found one:" + _ClosesestDonor.DisplayName, NotifyType.StatusMessage);
                // Launch notification?
                LaunchNotification(_ClosesestDonor);
                
            }
        }

        private async void LaunchNotification(PointOfInterest _FoundDonor)
        {
            // TODO ... App to App service to send data to EBTL Client application.
            ToastNotificationManager.History.Clear();
            //ToastHelper.PopCustomToast(NotificationPayload);

            // https://blogs.msdn.microsoft.com/tiles_and_toasts/2015/07/08/quickstart-sending-a-local-toast-notification-and-handling-activations-from-it-windows-10/
            // http://stackoverflow.com/questions/36068229/uwp-how-do-a-process-buttons-displayed-in-a-toast-that-are-launched-from-a-backg
            // http://fr.slideshare.net/shahedC3000/deeper-into-windows-10-development
            // https://blogs.msdn.microsoft.com/tiles_and_toasts/2015/07/02/adaptive-and-interactive-toast-notifications-for-windows-10/
            
            // This is the correct one.
            string BingMapsURI = @"bingmaps:?rtp=~pos." + _FoundDonor.Location.Position.Latitude.ToString()  + "_"  + _FoundDonor.Location.Position.Longitude.ToString();
            // Also correct.
            string DriveToURI = @"ms-drive-to:?destination.latitude=" + _FoundDonor.Location.Position.Latitude.ToString() + "&destination.longitude=" + _FoundDonor.Location.Position.Longitude.ToString();

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

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SearchClosestDonor("AB+");
        }

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

            ShowRouteData(routeResult);

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

        private void MainMapLoaded(object sender, RoutedEventArgs e)
        {
            MainMap.Center = _HospitalGeoPoint;

            MainMap.ZoomLevel = 12;
            _POIManager = new PointOfInterestsManager();

            // Create the databinding here.

            // Make a new source, to grab a new timestamp
            _Donors = new ObservableCollection<PointOfInterest>();
        }

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

        private void SetupNotificationContent()
        {
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
        }

        
        

    }
}