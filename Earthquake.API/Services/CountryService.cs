using System.Text.Json;
using Earthquake.DTO;
using Microsoft.Extensions.Caching.Memory;

namespace Earthquake.API.Services;

public interface ICountryService
{
    Task<CountryDto?> GetCountryAsync(string name);
}

public class CountryService : ICountryService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _cfg;
    private const string ApiKey = "rc_live_19b45c0ed5614f109c86256db20c11b5";

    public CountryService(HttpClient http, IMemoryCache cache, IConfiguration cfg)
    { _http = http; _cache = cache; _cfg = cfg; }

    public async Task<CountryDto?> GetCountryAsync(string name)
    {
        var key = $"country_{name.ToLower()}";
        if (_cache.TryGetValue(key, out CountryDto? cached)) return cached;

        // RestCountries v5
        var url = $"https://api.restcountries.com/countries/v5/names.common/{Uri.EscapeDataString(name)}";

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Authorization", $"Bearer {ApiKey}");

        try
        {
            var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                Console.WriteLine($"RestCountries respondió {(int)res.StatusCode}");
                return null;
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // La respuesta v5 tiene estructura { success, data: { objects: [...] } }
            if (!root.TryGetProperty("data", out var dataEl)) return null;
            if (!dataEl.TryGetProperty("objects", out var objectsEl)) return null;
            if (objectsEl.ValueKind != JsonValueKind.Array || objectsEl.GetArrayLength() == 0) return null;

            var c = objectsEl[0];

            // names.common
            string commonName = name;
            if (c.TryGetProperty("names", out var namesEl) &&
                namesEl.TryGetProperty("common", out var commonEl))
                commonName = commonEl.GetString() ?? name;

            // capitals[0].name
            string capital = "";
            if (c.TryGetProperty("capitals", out var capsEl) &&
                capsEl.ValueKind == JsonValueKind.Array &&
                capsEl.GetArrayLength() > 0)
            {
                var cap0 = capsEl[0];
                if (cap0.TryGetProperty("name", out var capNameEl))
                    capital = capNameEl.GetString() ?? "";
            }

            // population
            long population = 0;
            if (c.TryGetProperty("population", out var popEl) &&
                popEl.ValueKind == JsonValueKind.Number)
                population = popEl.GetInt64();

            var dto = new CountryDto
            {
                Name = commonName,
                Capital = capital,
                Population = population
            };
            _cache.Set(key, dto, TimeSpan.FromMinutes(30));
            return dto;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error RestCountries v5: {ex.Message}");
            return null;
        }
    }
}