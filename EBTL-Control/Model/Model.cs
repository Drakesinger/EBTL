using Microsoft.Data.Entity;
using Windows.Devices.Geolocation;

namespace EBLT_Control
{
    public class EBTLContext : DbContext
    {
        public DbSet<DonorDB> DonorsDB { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=EBTLDonors.db");
        }

        // Will see
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Make BlogDonor.BloodType required
            modelBuilder.Entity<DonorDB>()
                .Property(b => b.BloodType)
                .IsRequired();
        }
    }

    public class DonorDB
    {
        public int ID { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string BloodType { get; set; }
        public Geoposition GeoLocation { get; set; }

        // We need a simpler object for geopositioning.
        public Geopoint GeoPoint { get; set; }

        public string EmergencyNumber { get; set; }
    }
}