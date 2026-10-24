using Earthquake.Data;

namespace Earthquake.Repository;

public interface IEarthquakeRepository
{
    Task<bool> AnyAsync();
    Task<HashSet<string>> GetExternalIdsAsync();
    Task AddRangeAsync(IEnumerable<EarthquakeEntity> items);
    Task<List<EarthquakeEntity>> SearchByCountryAsync(string country);
    Task SaveChangesAsync();
}
