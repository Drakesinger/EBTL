using EBTL;
using System;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace EBTL_Control.ViewModel
{
    public class PointOfInterest
    {
        private Point _DefaultAnchorPoint = new Point(28, 87);

        public PointOfInterest()
        {
            this.NormalizedAnchorPoint = _DefaultAnchorPoint;
            this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        public PointOfInterest(Donor _Donor)
        {
            if (_Donor.GeoLocation != null)
            {
                this.Location = new Geopoint(new BasicGeoposition()
                {
                    Latitude = _Donor.GeoLocation.Coordinate.Latitude,
                    Longitude = _Donor.GeoLocation.Coordinate.Longitude
                });
            }
            else if (_Donor.GeoPoint != null)
            {
                this.Location = _Donor.GeoPoint;
            }

            this.DisplayName = _Donor.Name + " " + _Donor.Surname;
            this.ImageSourceUri = new Uri("ms-appx:///Assets/MapPin.png", UriKind.RelativeOrAbsolute);
            this.NormalizedAnchorPoint = _DefaultAnchorPoint;
            this.Address = _Donor.Address;
            this.BloodType = _Donor.BloodType;
            this.EmergencyNumber = _Donor.EmergencyNumber;

            this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        public string DisplayName { get; set; }
        public Geopoint Location { get; set; }
        public Uri ImageSourceUri { get; set; }
        public Point NormalizedAnchorPoint { get; set; }

        public string Address { get; private set; }
        public string BloodType { get; private set; }
        public string EmergencyNumber { get; private set; }
        public Visibility Visibility { get; set; }
    }
}