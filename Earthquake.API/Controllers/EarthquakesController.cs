using System.Text.RegularExpressions;
using Earthquake.API.Services;
using Earthquake.DTO;
using Earthquake.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Earthquake.API.Controllers;

[ApiController]
[Route("api/earthquakes")]
public class EarthquakesController : ControllerBase
{
    private readonly IEarthquakeRepository _repo;
    private readonly IUsgsService _usgs;
    private readonly ICountryService _countries;

    public EarthquakesController(
        IEarthquakeRepository repo,
        IUsgsService usgs,
        ICountryService countries)
    {
        _repo = repo;
        _usgs = usgs;
        _countries = countries;
    }

    // Cargar los registros de USGS.
    [HttpPost("load")]
    public async Task<IActionResult> Load(
        [FromQuery] bool force = false)
    {
        var nuevos = await _usgs.FetchAndPersistAsync(force);

        return Ok(new
        {
            inserted = nuevos.Count
        });
    }

    // Buscar sismos e información del país.
    [HttpGet("country/{country}")]
    public async Task<IActionResult> ByCountry(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return BadRequest(new
            {
                message = "El país es obligatorio."
            });
        }

        country = country.Trim();

        if (country.Length > 100)
        {
            return BadRequest(new
            {
                message = "El país no puede superar 100 caracteres."
            });
        }

        if (!Regex.IsMatch(country, @"^\p{L}+(?: \p{L}+)*$"))
        {
            return BadRequest(new
            {
                message = "Escribe solo letras y espacios."
            });
        }

        // Primero comprobar que el país sea reconocido.
        var countryInfo = await _countries.GetCountryAsync(country);

        if (countryInfo == null)
        {
            return NotFound(new
            {
                message = "País no encontrado."
            });
        }

        // Si ya hay registros en SQL, el servicio no descarga otra vez.
        await _usgs.FetchAndPersistAsync();

        // Buscar usando el nombre que devuelve RestCountries.
        var quakes = await _repo.SearchByCountryAsync(
            countryInfo.Name);

        var result = new SearchResultDto
        {
            Country = countryInfo,

            Earthquakes = quakes.Select(e => new EarthquakeDto
            {
                ExternalId = e.ExternalId,
                Place = e.Place,
                Magnitude = e.Magnitude,
                Time = e.Time,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                Country = e.Country
            }).ToList()
        };

        return Ok(result);
    }
}