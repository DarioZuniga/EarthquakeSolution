using Microsoft.EntityFrameworkCore;

namespace Earthquake.Data;

public class EarthquakeDbContext : DbContext
{
    public EarthquakeDbContext(DbContextOptions<EarthquakeDbContext> options)
        : base(options) { }

    public DbSet<EarthquakeEntity> Earthquakes => Set<EarthquakeEntity>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<EarthquakeEntity>(e =>
        {
            e.HasIndex(x => x.ExternalId).IsUnique();
            e.Property(x => x.ExternalId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Place).HasMaxLength(300);
            e.Property(x => x.Country).HasMaxLength(100);
        });
    }
}