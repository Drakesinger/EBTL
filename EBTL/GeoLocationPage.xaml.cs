using BackgroundTasks.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Devices.Geolocation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EBTL
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// https://msdn.microsoft.com/library/windows/apps/mt219698#get_the_current_location
    /// </summary>
    public sealed partial class GeoLocationPage : Page
    {
        private Donor _Donor;
        private uint _UpdateDelta = 7200; // 2 Hours.
        private CancellationTokenSource _cts;

        private AppServiceConnection connection;

        public GeoLocationPage()
        {
            this.InitializeComponent();
            InitializeLocalSettings();
        }

        private void InitializeLocalSettings()
        {
            ActivatedPage.localSettings = ApplicationData.Current.LocalSettings;
            ActivatedPage.localFolder = ApplicationData.Current.LocalFolder;
        }

        private void WriteSettings(AppStatus _AppStatus)
        {
            switch (_AppStatus)
            {
                case AppStatus.LocationEnabled:

                    ActivatedPage.localSettings.Values["AppConfigured"] = "1";
                    ActivatedPage.localSettings.Values["LocationEnabled"] = "1";
                    ActivatedPage.localSettings.Values["UseAddressAsLocation"] = "0";
                    ActivatedPage.localSettings.Values["PageToOpen"] = "ActivatedPage";

                    WriteDonorData();

                    break;

                case AppStatus.LocationDisabled:

                    ActivatedPage.localSettings.Values["AppConfigured"] = "1";
                    ActivatedPage.localSettings.Values["LocationEnabled"] = "0";
                    ActivatedPage.localSettings.Values["UseAddressAsLocation"] = "1";
                    ActivatedPage.localSettings.Values["PageToOpen"] = "ActivatedPage";

                    WriteDonorData();

                    break;

                case AppStatus.LocationUnknown:

                    ActivatedPage.localSettings.Values["AppConfigured"] = "0";
                    ActivatedPage.localSettings.Values["LocationEnabled"] = "0";
                    ActivatedPage.localSettings.Values["UseAddressAsLocation"] = "0";
                    ActivatedPage.localSettings.Values["PageToOpen"] = "MainPage";

                    break;

                default:
                    break;
            }
        }

        private void WriteDonorData()
        {
            ActivatedPage.localSettings.Values["Donor.BloodType"] = _Donor.BloodType;
            ActivatedPage.localSettings.Values["Donor.EmergencyNumber"] = _Donor.EmergencyNumber;
            ActivatedPage.localSettings.Values["Donor.Address"] = _Donor.Address;
            ActivatedPage.localSettings.Values["Donor.Name"] = _Donor.Name;
            ActivatedPage.localSettings.Values["Donor.Surname"] = _Donor.Surname;
        }

        private async void InitializeGeoLocation(ToggleSwitch _Sender)
        {
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
                        HideLocationDisablesInformation();

                        // Hide the information text block as the user allowed location services.
                        textBlock_Information.Visibility = Visibility.Collapsed;
                        // Make a toast to say thank you.
                        stackPanel_Setup.Visibility = Visibility.Visible;

                        // Get cancellation token
                        _cts = new CancellationTokenSource();
                        CancellationToken token = _cts.Token;

                        Geolocator geolocator = new Geolocator
                        {
                            // Define period.
                            ReportInterval = _UpdateDelta
                        };

                        // Subscribe to the PositionChanged event to get location updates.
                        geolocator.PositionChanged += OnPositionChanged;

                        // Subscribe to StatusChanged event to get updates of location status changes.
                        geolocator.StatusChanged += OnStatusChanged;

                        LocationDisabledMessage.Visibility = Visibility.Collapsed;

                        // Carry out the operation
                        Geoposition pos = await geolocator.GetGeopositionAsync().AsTask(token);

                        UpdateLocationData(pos);

                        // The app has been configured.
                        WriteSettings(AppStatus.LocationEnabled);

                        // Send data to server.
                        OpenConnection();
                        GenerateMessage();
                        CloseConnection();
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
        }

        private void HideLocationDisablesInformation()
        {
            LocationDisabledMessage.Visibility = Visibility.Collapsed;
        }

        private void InformUserOfUnspecifiedChoice()
        {
            WriteSettings(AppStatus.LocationUnknown);
            LocationDisabledMessage.Visibility = Visibility.Visible;
        }

        private void InformUserOfDeniedChoice()
        {
            // Use address as location.
            WriteSettings(AppStatus.LocationDisabled);

            // Set the link to settings to visible.
            LocationDisabledMessage.Visibility = Visibility.Visible;
        }

        private void UpdateLocationData(Geoposition pos)
        {
            if (pos != null)
            {
                textBlock_Lat.Text = pos.Coordinate.Latitude.ToString();
                textBlock_Long.Text = pos.Coordinate.Longitude.ToString();

                // Update donor data.
                _Donor.GeoLocation = pos;
                _Donor.GeoPoint = new Geopoint(new BasicGeoposition()
                {
                    Latitude = pos.Coordinate.Latitude,
                    Longitude = pos.Coordinate.Longitude
                });
            }
            else
            {
                textBlock_Lat.Text = "Lat";
                textBlock_Long.Text = "Long";
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Donor)
            {
                _Donor = e.Parameter as Donor;
            }

            base.OnNavigatedTo(e);
        }

        async private void OnStatusChanged(Geolocator sender, StatusChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Show the location setting message only if status is disabled.
                LocationDisabledMessage.Visibility = Visibility.Collapsed;

                switch (e.Status)
                {
                    case PositionStatus.Ready:
                        // Location platform is providing valid data.
                        break;

                    case PositionStatus.Initializing:
                        // Location platform is attempting to acquire a fix.
                        break;

                    case PositionStatus.NoData:
                        // Location platform could not obtain location data.
                        break;

                    case PositionStatus.Disabled:
                        // The permission to access location data is denied by the user or other policies.

                        // Show message to the user to go to location settings.
                        LocationDisabledMessage.Visibility = Visibility.Visible;

                        // Clear any cached location data.
                        UpdateLocationData(null);
                        break;

                    case PositionStatus.NotInitialized:
                        // The location platform is not initialized.
                        // This indicates that the application has not made a request for location data.
                        break;

                    case PositionStatus.NotAvailable:
                        // The location platform is not available on this version of the OS.
                        break;

                    default:
                        //ScenarioOutput_Status.Text = "Unknown";
                        break;
                }
            });
        }

        async private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateLocationData(args.Position);
            });
        }

        private void toggleSwitch_Update_Toggled(object sender, RoutedEventArgs e)
        {
            var _ToggleSwitch = sender as ToggleSwitch;
            if (_ToggleSwitch.IsOn)
            {
                // Ask user for Geolocation System permission.
                InitializeGeoLocation(_ToggleSwitch);
            }
            else
            {
                stackPanel_Setup.Visibility = Visibility.Collapsed;
            }
        }

        private async void OpenConnection()
        {
            //Is a connection already open?
            if (connection != null)
            {
                //rootPage.NotifyUser("A connection already exists", NotifyType.ErrorMessage);
                return;
            }

            //Set up a new app service connection
            connection = new AppServiceConnection();
            connection.AppServiceName = "com.he-arc.contentprovider";
            connection.PackageFamilyName = "212a2e96-fa51-46b6-b0e7-46e21d5029c5_1.0.0.0_x86__kswx83yzr7kvy";
            connection.ServiceClosed += Connection_ServiceClosed;
            AppServiceConnectionStatus status = await connection.OpenAsync();

            //If the new connection opened successfully we're done here
            if (status == AppServiceConnectionStatus.Success)
            {
                //rootPage.NotifyUser("Connection is open", NotifyType.StatusMessage);
            }
            else
            {
                //Something went wrong. Lets figure out what it was and show the user a meaningful message.
                switch (status)
                {
                    case AppServiceConnectionStatus.AppNotInstalled:
                        //rootPage.NotifyUser("The app AppServicesProvider is not installed. Deploy AppServicesProvider to this device and try again.", NotifyType.ErrorMessage);
                        break;

                    case AppServiceConnectionStatus.AppUnavailable:
                        //rootPage.NotifyUser("The app AppServicesProvider is not available. This could be because it is currently being updated or was installed to a removable device that is no longer available.", NotifyType.ErrorMessage);
                        break;

                    case AppServiceConnectionStatus.AppServiceUnavailable:
                        //rootPage.NotifyUser(string.Format("The app AppServicesProvider is installed but it does not provide the app service {0}.", connection.AppServiceName), NotifyType.ErrorMessage);
                        break;

                    case AppServiceConnectionStatus.Unknown:
                        //rootPage.NotifyUser("An unkown error occurred while we were trying to open an AppServiceConnection.", NotifyType.ErrorMessage);
                        break;
                }

                //Clean up before we go
                connection.Dispose();
                connection = null;
            }
        }

        private async void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //Dispose the connection reference we're holding
                if (connection != null)
                {
                    connection.Dispose();
                    connection = null;
                }
            });
        }

        private void CloseConnection()
        {
            //Is there an open connection?
            if (connection == null)
            {
                return;
            }

            //Close the open connection
            connection.Dispose();
            connection = null;
        }

        private async void GenerateMessage()
        {
            if (_Donor == null)
            {
                // Oh oh.
                return;
            }
            //Is there an open connection?
            if (connection == null)
            {
                //rootPage.NotifyUser("You need to open a connection before trying to generate a random number.", NotifyType.ErrorMessage);
                return;
            }

            //Send a message to the app service
            var inputs = new ValueSet();
            inputs.Add("Surname", _Donor.Surname);
            inputs.Add("Name", _Donor.Name);
            inputs.Add("Address", _Donor.Address);
            inputs.Add("BloodType", _Donor.BloodType);
            inputs.Add("GeoLocation", _Donor.GeoLocation);
            inputs.Add("GeoPoint", _Donor.GeoPoint);
            inputs.Add("EmergencyNumber", _Donor.EmergencyNumber);
            AppServiceResponse response = await connection.SendMessageAsync(inputs);

            //If the service responded display the message. We're done!
            if (response.Status == AppServiceResponseStatus.Success)
            {
                ToastNotificationManager.History.Clear();
                ToastHelper.PopToast("Data received!!!", "YESSS");

                //if (!response.Message.ContainsKey("result"))
                //{
                //    rootPage.NotifyUser("The app service response message does not contain a key called \"result\"", NotifyType.StatusMessage);
                //    return;
                //}

                //var resultText = response.Message["result"].ToString();
                //if (!string.IsNullOrEmpty(resultText))
                //{
                //    Result.Text = resultText;
                //    rootPage.NotifyUser("App service responded with a result", NotifyType.StatusMessage);
                //}
                //else
                //{
                //    rootPage.NotifyUser("App service did not respond with a result", NotifyType.ErrorMessage);
                //}
            }
            else
            {
                ToastNotificationManager.History.Clear();
                ToastHelper.PopToast("Damn", "NOOOOO");

                //Something went wrong. Show the user a meaningful
                //message depending upon the status

                switch (response.Status)
                {
                    case AppServiceResponseStatus.Failure:
                        //rootPage.NotifyUser("The service failed to acknowledge the message we sent it. It may have been terminated because the client was suspended.", NotifyType.ErrorMessage);
                        break;

                    case AppServiceResponseStatus.ResourceLimitsExceeded:
                        //rootPage.NotifyUser("The service exceeded the resources allocated to it and had to be terminated.", NotifyType.ErrorMessage);
                        break;

                    case AppServiceResponseStatus.Unknown:
                    default:
                        //rootPage.NotifyUser("An unkown error occurred while we were trying to send a message to the service.", NotifyType.ErrorMessage);
                        break;
                }
            }
        }

        private enum AppStatus
        {
            LocationEnabled,
            LocationDisabled,
            LocationUnknown,
        }
    }
}