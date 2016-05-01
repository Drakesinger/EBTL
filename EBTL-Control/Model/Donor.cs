using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace EBTL_Control
{
    public class Donor
    {
        public Donor(string Surname, string Name, string ContactNumber, string Address, string BloodType)
        {
            this.Surname = Surname;
            this.Name = Name;
            this.Address = Address;
            this.BloodType = BloodType;
            this.EmergencyNumber = ContactNumber;

            // Check if this is how we want to do it.
            this.GeoLocation = null;
            this.GeoPoint = null;
        }

        public string Surname { get; private set; }
        public string Name { get; private set; }
        public string Address { get; private set; }
        public string BloodType { get; private set; }
        public Geoposition GeoLocation { get; set; }

        // We need a simpler object for geopositioning.
        public Geopoint GeoPoint { get; set; }

        public string EmergencyNumber { get; private set; }
    }
}