using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Earthquake.Data;

public class EarthquakeDbContextFactory : IDesignTimeDbContextFactory<EarthquakeDbContext>
{
    public EarthquakeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EarthquakeDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=EarthquakeDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new EarthquakeDbContext(options);
    }
}