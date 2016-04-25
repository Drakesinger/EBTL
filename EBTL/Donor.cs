using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBTL
{
    public class Donor
    {
        public Donor(string Surname, string Name, string Address, string BloodType)
        {
            this.Surname = Surname;
            this.Name = Name;
            this.Address = Address;
            this.BloodType = BloodType;
        }

        public string Surname { get; private set; }
        public string Name { get; private set; }
        public string Address { get; private set; }
        public string BloodType { get; private set; }
    }
}
