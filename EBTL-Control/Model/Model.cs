using Microsoft.Data.Entity;
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace EBLT_Control
{
    public class EBTLContext : DbContext
    {
        public DbSet<Donor> Blogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=EBTLDonors.db");
        }

        // Will see
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }

    public class Donor
    {
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