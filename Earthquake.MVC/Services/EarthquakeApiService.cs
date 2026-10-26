using Earthquake.DTO;
using Microsoft.Extensions.Caching.Memory;

namespace Earthquake.MVC.Services;

public interface IEarthquakeApiService
{
    Task InitializeAsync();

    Task<SearchResultDto> SearchAsync(string country);
}

public class EarthquakeApiService : IEarthquakeApiService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public EarthquakeApiService(
        HttpClient http,
        IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    // Solicitar la preparación de los datos al abrir la aplicación.
    public async Task InitializeAsync()
    {
        if (_cache.TryGetValue("earthquakes_ready", out bool ready)
            && ready)
        {
            return;
        }

        using var response = await _http.PostAsync(
            "api/earthquakes/load?force=false",
            null);

        response.EnsureSuccessStatusCode();

        _cache.Set(
            "earthquakes_ready",
            true,
            TimeSpan.FromMinutes(30));
    }

    // Buscar los sismos y la información de un país.
    public async Task<SearchResultDto> SearchAsync(string country)
    {
        country = country.Trim();

        var cacheKey = "search_" + country.ToLowerInvariant();

        if (_cache.TryGetValue(
                cacheKey,
                out SearchResultDto? cached)
            && cached != null)
        {
            return cached;
        }

        var url = "api/earthquakes/country/"
            + Uri.EscapeDataString(country);

        var result = await _http
            .GetFromJsonAsync<SearchResultDto>(url);

        if (result == null || result.Country == null)
        {
            throw new InvalidOperationException(
                "La API no devolvió información válida del país.");
        }

        _cache.Set(
            cacheKey,
            result,
            TimeSpan.FromMinutes(10));

        return result;
    }
}