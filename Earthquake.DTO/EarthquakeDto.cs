namespace Earthquake.DTO;

public class EarthquakeDto
{
    public string ExternalId { get; set; } = "";
    public string Place { get; set; } = "";
    public double Magnitude { get; set; }
    public long Time { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Country { get; set; } = "";
}