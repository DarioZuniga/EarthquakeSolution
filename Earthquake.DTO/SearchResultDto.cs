namespace Earthquake.DTO;

public class SearchResultDto
{
    public CountryDto? Country { get; set; }
    public List<EarthquakeDto> Earthquakes { get; set; } = new();
}