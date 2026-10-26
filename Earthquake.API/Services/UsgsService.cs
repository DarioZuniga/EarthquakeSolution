using System.Text.Json;
using Earthquake.Data;
using Earthquake.DTO;
using Earthquake.Repository;
using Microsoft.Extensions.Caching.Memory;

namespace Earthquake.API.Services;

public interface IUsgsService
{
    Task<List<EarthquakeDto>> FetchAndPersistAsync(bool force = false);
}

public class UsgsService : IUsgsService
{
    private readonly HttpClient _http;
    private readonly IEarthquakeRepository _repo;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _cfg;

    public UsgsService(
        HttpClient http,
        IEarthquakeRepository repo,
        IMemoryCache cache,
        IConfiguration cfg)
    {
        _http = http;
        _repo = repo;
        _cache = cache;
        _cfg = cfg;
    }

    public async Task<List<EarthquakeDto>> FetchAndPersistAsync(
        bool force = false)
    {
        // Si ya existen datos, una carga normal utiliza SQL.
        if (!force && await _repo.AnyAsync())
        {
            return new List<EarthquakeDto>();
        }

        var baseUrl = _cfg["Usgs:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Falta configurar Usgs:BaseUrl.");
        }

        var existingIds = await _repo.GetExternalIdsAsync();
        var nuevos = new List<EarthquakeEntity>();

        const int cantidad = 20000;
        int offset = 1;
        int recibidos;

        do
        {
            // Preparar una página de resultados.
            var url = baseUrl
                + "&orderby=time-asc"
                + "&limit=" + cantidad
                + "&offset=" + offset;

            var cacheKey = "usgs_" + url;

            // Reutilizar la respuesta si está en caché.
            if (!_cache.TryGetValue(cacheKey, out string? json))
            {
                json = await _http.GetStringAsync(url);

                _cache.Set(
                    cacheKey,
                    json,
                    TimeSpan.FromHours(1));
            }

            using var document = JsonDocument.Parse(json!);

            var features = document.RootElement
                .GetProperty("features");

            recibidos = features.GetArrayLength();

            foreach (var feature in features.EnumerateArray())
            {
                var externalId = feature.GetProperty("id")
                    .GetString() ?? "";

                // Add devuelve false si el ID ya estaba registrado.
                if (string.IsNullOrWhiteSpace(externalId)
                    || !existingIds.Add(externalId))
                {
                    continue;
                }

                var properties = feature.GetProperty("properties");

                var coordinates = feature.GetProperty("geometry")
                    .GetProperty("coordinates");

                var place = properties.GetProperty("place")
                    .GetString() ?? "";

                double magnitude = 0;

                if (properties.TryGetProperty("mag", out var mag)
                    && mag.ValueKind == JsonValueKind.Number)
                {
                    magnitude = mag.GetDouble();
                }

                nuevos.Add(new EarthquakeEntity
                {
                    ExternalId = externalId,
                    Place = place,
                    Magnitude = magnitude,
                    Time = properties.GetProperty("time").GetInt64(),
                    Longitude = coordinates[0].GetDouble(),
                    Latitude = coordinates[1].GetDouble(),
                    Country = ExtractCountry(place)
                });
            }

            offset += cantidad;

        } while (recibidos == cantidad);

        // Guardar después de completar la descarga.
        if (nuevos.Count > 0)
        {
            await _repo.AddRangeAsync(nuevos);
            await _repo.SaveChangesAsync();
        }

        return nuevos.Select(Map).ToList();
    }

    private static string ExtractCountry(string place)
    {
        var posicion = place.LastIndexOf(',');

        if (posicion >= 0)
        {
            return place[(posicion + 1)..].Trim();
        }

        return place.Trim();
    }

    private static EarthquakeDto Map(EarthquakeEntity entity)
    {
        return new EarthquakeDto
        {
            ExternalId = entity.ExternalId,
            Place = entity.Place,
            Magnitude = entity.Magnitude,
            Time = entity.Time,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            Country = entity.Country
        };
    }
}