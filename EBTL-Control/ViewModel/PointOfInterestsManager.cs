using EBTL;
using EBTL_Control.Model;
using System;
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace EBTL_Control.ViewModel
{
    public class PointOfInterestsManager
    {
        public List<PointOfInterest> FetchPOIs(BasicGeoposition center)
        {
            List<PointOfInterest> pois = new List<PointOfInterest>();
            pois.Add(new PointOfInterest()
            {
                DisplayName = "Place One",
                ImageSourceUri = new Uri("ms-appx:///Assets/MapPin.png", UriKind.RelativeOrAbsolute),
                Location = new Geopoint(new BasicGeoposition()
                {
                    Latitude = center.Latitude + 0.001,
                    Longitude = center.Longitude - 0.001
                })
            });
            pois.Add(new PointOfInterest()
            {
                DisplayName = "Place Two",
                ImageSourceUri = new Uri("ms-appx:///Assets/MapPin.png", UriKind.RelativeOrAbsolute),
                Location = new Geopoint(new BasicGeoposition()
                {
                    Latitude = center.Latitude + 0.001,
                    Longitude = center.Longitude + 0.001
                })
            });
            pois.Add(new PointOfInterest()
            {
                DisplayName = "Place Three",
                ImageSourceUri = new Uri("ms-appx:///Assets/MapPin.png", UriKind.RelativeOrAbsolute),
                Location = new Geopoint(new BasicGeoposition()
                {
                    Latitude = center.Latitude - 0.001,
                    Longitude = center.Longitude - 0.001
                })
            });
            pois.Add(new PointOfInterest()
            {
                DisplayName = "Place Four",
                ImageSourceUri = new Uri("ms-appx:///Assets/MapPin.png", UriKind.RelativeOrAbsolute),
                Location = new Geopoint(new BasicGeoposition()
                {
                    Latitude = center.Latitude - 0.001,
                    Longitude = center.Longitude + 0.001
                })
            });
            return pois;
        }

        public List<PointOfInterest> AddDonor(Donor _Donor)
        {
            List<PointOfInterest> pois = new List<PointOfInterest>();
            pois.Add(new PointOfInterest(_Donor));
            return pois;
        }

        public List<PointOfInterest> AddDonor(Donor _Donor, object itemsSource)
        {
            if (itemsSource != null)
            {
                Type _type = itemsSource.GetType();

                var _lists = itemsSource as List<PointOfInterest>;

                _lists.Add(new PointOfInterest(_Donor));
                return _lists;
            }
            else
            {
                return AddDonor(_Donor);
            }
        }
    }
}