using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

public class EventTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EventTests(RaceDayApiFactory factory) => _factory = factory;

    private static EventRequest SampleEvent(string name = "Sample Event") => new()
    {
        Name = name,
        Description = "Description",
        Location = "Cape Town",
        EventDate = DateTime.UtcNow.AddMonths(1)
    };

    // --- Event management (Organiser success paths) ---

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var (organiser, user) = await TestHelpers.CreateLoggedInClientAsync(_factory, "org.create@test.com", "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", SampleEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ev = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal(user.UserId, ev!.OrganiserId);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "org.update@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}", SampleEvent("Renamed Event"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed Event", updated!.Name);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_Returns204()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "org.delete@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var afterDelete = await organiser.GetAsync($"/api/events/{ev.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Events_PublicListAndDetail_NoLoginRequired()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "org.public@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);

        var anonymous = _factory.CreateClient();

        var list = await anonymous.GetAsync("/api/events");
        var detail = await anonymous.GetAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task GetEvent_NonExistent_Returns404()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Role-based rejection ---

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "part.create@test.com", "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", SampleEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateEvent_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/events", SampleEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "owner@test.com", "Organiser");
        var (other, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "other.org@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var response = await other.PutAsJsonAsync($"/api/events/{ev.EventId}", SampleEvent("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "owner2@test.com", "Organiser");
        var (other, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "other.org2@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var response = await other.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotAccessParticipantOnlyEndpoint_Returns403()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "org.enrolments@test.com", "Organiser");

        var response = await organiser.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
