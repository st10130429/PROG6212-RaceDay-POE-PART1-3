namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }

    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }

    // Part 3 event presentation fields
    public string EventType { get; set; } = "Running";
    public decimal DistanceKm { get; set; }
    public string? BannerUrl { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
