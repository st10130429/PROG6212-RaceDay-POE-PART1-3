using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

public static class TestHelpers
{
    /// <summary>Registers a user and logs in, leaving the session cookie on the returned client.</summary>
    public static async Task<(HttpClient Client, UserResponse User)> CreateLoggedInClientAsync(
        RaceDayApiFactory factory, string email, string role, string password = "Passw0rd!")
    {
        var client = factory.CreateClient(); // each client has its own cookie container (own session)

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Name = email.Split('@')[0],
            Email = email,
            Password = password,
            Role = role
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        login.EnsureSuccessStatusCode();

        var user = (await login.Content.ReadFromJsonAsync<UserResponse>())!;
        return (client, user);
    }

    public static async Task<EventResponse> CreateEventAsync(HttpClient organiser, string name = "City 10K")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new EventRequest
        {
            Name = name,
            Description = "Annual road race",
            Location = "Johannesburg",
            EventDate = DateTime.UtcNow.AddMonths(2)
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryRequest
        {
            Name = "10km Senior",
            DistanceKm = 10,
            Price = 150
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }
}
