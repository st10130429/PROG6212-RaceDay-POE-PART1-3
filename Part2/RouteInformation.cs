namespace RaceDay.Api.Models;

public class RouteInformation
{
    public int RouteId { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal DistanceKm { get; set; }
    public decimal ElevationGain { get; set; }
    public string MapUrl { get; set; } = string.Empty;
}
