using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Required] public string Location { get; set; } = string.Empty;
    [Required] public DateTime EventDate { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
}

public class CategoryRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Range(0, 1000)] public decimal DistanceKm { get; set; }
    [Range(0, 100000)] public decimal Price { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal Price { get; set; }
}

public class RouteRequest
{
    [Range(0, 1000)] public decimal DistanceKm { get; set; }
    [Range(0, 10000)] public decimal ElevationGain { get; set; }
    public string MapUrl { get; set; } = string.Empty;
}

public class RouteResponse
{
    public int RouteId { get; set; }
    public int CategoryId { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal ElevationGain { get; set; }
    public string MapUrl { get; set; } = string.Empty;
}

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }
}

public class ResultRequest
{
    [Required] public TimeSpan FinishTime { get; set; }
    [Range(1, 100000)] public int Position { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}
