using System.Text.Json;
using Earthquake.Data;
using Earthquake.DTO;
using Earthquake.Repository;
using Microsoft.Extensions.Caching.Memory;

namespace Earthquake.API.Services;

public interface IUsgsService
{
    Task<List<EarthquakeDto>> FetchAndPersistAsync();
}

public class UsgsService : IUsgsService
{
    private readonly HttpClient _http;
    private readonly IEarthquakeRepository _repo;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _cfg;

    public UsgsService(HttpClient http, IEarthquakeRepository repo,
        IMemoryCache cache, IConfiguration cfg)
    { _http = http; _repo = repo; _cache = cache; _cfg = cfg; }

    public async Task<List<EarthquakeDto>> FetchAndPersistAsync()
    {
        if (_cache.TryGetValue("usgs_fetched", out _))
            return new List<EarthquakeDto>();

        var url = _cfg["Usgs:BaseUrl"]!;
        var json = await _http.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        var features = doc.RootElement.GetProperty("features");

        var existing = await _repo.GetExternalIdsAsync();
        var nuevos = new List<EarthquakeEntity>();

        foreach (var f in features.EnumerateArray())
        {
            var id = f.GetProperty("id").GetString() ?? "";
            if (existing.Contains(id)) continue;

            var props = f.GetProperty("properties");
            var coords = f.GetProperty("geometry").GetProperty("coordinates");
            var place = props.GetProperty("place").GetString() ?? "";

            double mag = 0;
            if (props.TryGetProperty("mag", out var magEl) &&
                magEl.ValueKind == JsonValueKind.Number)
                mag = magEl.GetDouble();

            nuevos.Add(new EarthquakeEntity
            {
                ExternalId = id,
                Place = place,
                Magnitude = mag,
                Time = props.GetProperty("time").GetInt64(),
                Longitude = coords[0].GetDouble(),
                Latitude = coords[1].GetDouble(),
                Country = ExtractCountry(place)
            });
        }

        if (nuevos.Count > 0)
        {
            await _repo.AddRangeAsync(nuevos);
            await _repo.SaveChangesAsync();
        }

        _cache.Set("usgs_fetched", true, TimeSpan.FromHours(1));
        return nuevos.Select(Map).ToList();
    }

    private static string ExtractCountry(string place)
    {
        var idx = place.LastIndexOf(',');
        return idx >= 0 ? place[(idx + 1)..].Trim() : place.Trim();
    }

    private static EarthquakeDto Map(EarthquakeEntity e) => new()
    {
        ExternalId = e.ExternalId,
        Place = e.Place,
        Magnitude = e.Magnitude,
        Time = e.Time,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        Country = e.Country
    };
}