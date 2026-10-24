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

    public EarthquakesController(IEarthquakeRepository repo,
        IUsgsService usgs, ICountryService countries)
    { _repo = repo; _usgs = usgs; _countries = countries; }

    [HttpGet("country/{country}")]
    public async Task<IActionResult> ByCountry(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return BadRequest(new { message = "País requerido" });

        await _usgs.FetchAndPersistAsync();

        var countryInfo = await _countries.GetCountryAsync(country);
        if (countryInfo is null)
            return NotFound(new { message = "País no encontrado" });

        var quakes = await _repo.SearchByCountryAsync(country);

        return Ok(new SearchResultDto
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
        });
    }
}