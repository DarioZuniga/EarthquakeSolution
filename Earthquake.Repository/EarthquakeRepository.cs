using Earthquake.Data;
using Microsoft.EntityFrameworkCore;

namespace Earthquake.Repository;

public class EarthquakeRepository : IEarthquakeRepository
{
    private readonly EarthquakeDbContext _db;

    public EarthquakeRepository(EarthquakeDbContext db) => _db = db;

    public Task<bool> AnyAsync() => _db.Earthquakes.AnyAsync();

    public async Task<HashSet<string>> GetExternalIdsAsync()
    {
        var ids = await _db.Earthquakes.Select(e => e.ExternalId).ToListAsync();
        return ids.ToHashSet();
    }

    public async Task AddRangeAsync(IEnumerable<EarthquakeEntity> items)
        => await _db.Earthquakes.AddRangeAsync(items);

    public Task<List<EarthquakeEntity>> SearchByCountryAsync(string country)
        => _db.Earthquakes
              .Where(e => e.Country.ToLower() == country.ToLower())
              .ToListAsync();

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}