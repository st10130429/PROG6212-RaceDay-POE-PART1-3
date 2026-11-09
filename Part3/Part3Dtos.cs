using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventDetailResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string EventType { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string? BannerUrl { get; set; }
}

public class EventMetaRequest
{
    [Required] public string EventType { get; set; } = "Running";
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; }
}

public class StatusRequest { [Required] public string Status { get; set; } = ""; }

public class DashboardItemResponse
{
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public int EnrolmentCount { get; set; }
}

public class EntrantResponse
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = "";
    public string ParticipantEmail { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string Status { get; set; } = "";
    public bool HasResult { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class MyEnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public string Status { get; set; } = "";
}

public class MyResultResponse
{
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public int TotalFinishers { get; set; }
}

public class UserProfileResponse
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}
