using System.Net;
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

    public CountryService(
        HttpClient http,
        IMemoryCache cache,
        IConfiguration cfg)
    {
        _http = http;
        _cache = cache;
        _cfg = cfg;
    }

    public async Task<CountryDto?> GetCountryAsync(string name)
    {
        // 1. Preparar el nombre y la clave de caché.
        name = name.Trim();

        var cacheKey = "country_" + name.ToLowerInvariant();

        // 2. Revisar si tenemos la información en memoria.
        if (_cache.TryGetValue(cacheKey, out CountryDto? cached)
            && cached != null)
        {
            return cached;
        }

        // 3. Leer la configuración de appsettings.json.
        var baseUrl = _cfg["RestCountries:BaseUrl"];
        var apiKey = _cfg["RestCountries:ApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Falta configurar RestCountries:BaseUrl.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Falta configurar RestCountries:ApiKey.");
        }

        // 4. Construir la dirección de búsqueda.
        var url = baseUrl.TrimEnd('/')
            + "/names.common/"
            + Uri.EscapeDataString(name);

        // 5. Preparar la solicitud con la clave.
        using var request = new HttpRequestMessage(
            HttpMethod.Get, url);

        request.Headers.Add(
            "Authorization", "Bearer " + apiKey);

        // 6. Enviar la solicitud.
        using var response = await _http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        // 7. Leer el JSON recibido.
        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        if (!root.TryGetProperty("data", out var data))
        {
            throw new InvalidOperationException(
                "RestCountries devolvió una respuesta inesperada.");
        }

        if (!data.TryGetProperty("objects", out var countries)
            || countries.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "La respuesta no contiene la lista de países.");
        }

        if (countries.GetArrayLength() == 0)
        {
            return null;
        }

        var country = countries[0];

        // 8. Obtener el nombre del país.
        string countryName = name;

        if (country.TryGetProperty("names", out var names)
            && names.TryGetProperty("common", out var common))
        {
            countryName = common.GetString() ?? name;
        }

        // 9. Obtener la capital.
        string capital = "";

        if (country.TryGetProperty("capitals", out var capitals)
            && capitals.ValueKind == JsonValueKind.Array
            && capitals.GetArrayLength() > 0)
        {
            var firstCapital = capitals[0];

            if (firstCapital.TryGetProperty("name", out var capitalName))
            {
                capital = capitalName.GetString() ?? "";
            }
        }

        // 10. Obtener la población.
        long population = 0;

        if (country.TryGetProperty("population", out var populationValue)
            && populationValue.ValueKind == JsonValueKind.Number)
        {
            population = populationValue.GetInt64();
        }

        // 11. Crear el DTO que utiliza nuestra aplicación.
        var result = new CountryDto
        {
            Name = countryName,
            Capital = capital,
            Population = population
        };

        // 12. Guardar el resultado durante 30 minutos.
        _cache.Set(
            cacheKey,
            result,
            TimeSpan.FromMinutes(30));

        return result;
    }
}