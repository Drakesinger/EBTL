using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;


using System.Threading;
using Windows.Devices.Geolocation;
using System.Threading.Tasks;

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
        private int _UpdateDelta;
        private CancellationTokenSource _cts;

        public GeoLocationPage()
        {
            this.InitializeComponent();
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
                        // Make a toast to say thank you.
                        stackPanel_Setup.Visibility = Visibility.Visible;


                        // Get cancellation token
                        _cts = new CancellationTokenSource();
                        CancellationToken token = _cts.Token;

                        //_rootPage.NotifyUser("Waiting for update...", NotifyType.StatusMessage);

                        // If DesiredAccuracy or DesiredAccuracyInMeters are not set (or value is 0), DesiredAccuracy.Default is used.
                        Geolocator geolocator = new Geolocator();

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
            catch(Exception)
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
            //throw new NotImplementedException();
        }

        private void InformUserOfDeniedChoice()
        {
            //throw new NotImplementedException();
        }

        private void UpdateLocationData(Geoposition pos)
        {
            textBlock_Lat.Text = pos.Coordinate.Latitude.ToString();
            textBlock_Long.Text = pos.Coordinate.Longitude.ToString();       
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {


            if (e.Parameter is Donor)
            {
                _Donor = e.Parameter as Donor;
            }

            base.OnNavigatedTo(e);
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
    }
}
