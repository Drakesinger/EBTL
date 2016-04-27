using EBTL_Control.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EBTL_Control
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CancellationTokenSource _cts;
        private uint _UpdateDelta = 7200; // 2 Hours.

        private Geoposition _HospitalGeoLocation;
        private Geopoint _HospitalGeoPoint;
        private PointOfInterestsManager _POIManager;

        public MainPage()
        {
            this.InitializeComponent();

            InitializeLocationService();

            MainMap.Loaded += MainMapLoaded;
            MainMap.MapTapped += MainMap_MapTapped;
        }

        private void MainMap_MapTapped(Windows.UI.Xaml.Controls.Maps.MapControl sender, Windows.UI.Xaml.Controls.Maps.MapInputEventArgs args)
        {
            var tappedGeoPosition = args.Location.Position;
            string status = "MapTapped at \nLatitude:" + tappedGeoPosition.Latitude + "\nLongitude: " + tappedGeoPosition.Longitude;
            //rootPage.NotifyUser(status, NotifyType.StatusMessage);
            // Will use this to get the information.
        }

        private void addXamlChildrenButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // MapItems.ItemsSource = _POIManager.FetchPOIs(MainMap.Center.Position);
        }

        private void mapItemButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var buttonSender = sender as Button;
            PointOfInterest poi = buttonSender.DataContext as PointOfInterest;
            //rootPage.NotifyUser("PointOfInterest clicked = " + poi.DisplayName, NotifyType.StatusMessage);
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
                        geolocator.PositionChanged += OnPositionChanged;

                        // Subscribe to StatusChanged event to get updates of location status changes.
                        geolocator.StatusChanged += OnStatusChanged;

                        //_rootPage.NotifyUser("Waiting for update...", NotifyType.StatusMessage);
                        //LocationDisabledMessage.Visibility = Visibility.Collapsed;

                        // Carry out the operation

                        Geoposition pos = await geolocator.GetGeopositionAsync().AsTask(token);

                        UpdateLocationData(pos);
                        // _rootPage.NotifyUser("Location updated.", NotifyType.StatusMessage);

                        break;

                    case GeolocationAccessStatus.Denied:
                        InformUserOfDeniedChoice();
                        // Inform the user that we will use his address for location but that his decision may cost a life.
                        // Battery before lives no?
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

                // Update donor data.
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
        }
    }
}