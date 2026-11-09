using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

/// <summary>
/// Integration tests for the Part 3 endpoints used by the MVC application.
/// These boot the real API pipeline and use EF Core InMemory plus a fake Blob service.
/// </summary>
public class Part3ApiTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public Part3ApiTests(RaceDayApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Browse_ReturnsPart3EventFields()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.browse.org@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser, "Part 3 10K");

        var meta = await organiser.PutAsJsonAsync($"/api/events/{ev.EventId}/meta",
            new EventMetaRequest { EventType = "Running", DistanceKm = 10 });
        Assert.Equal(HttpStatusCode.OK, meta.StatusCode);

        var response = await _factory.CreateClient().GetAsync("/api/events/browse");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventDetailResponse>>();
        var listed = Assert.Single(events!, x => x.EventId == ev.EventId);
        Assert.Equal("Running", listed.EventType);
        Assert.Equal(10m, listed.DistanceKm);
    }

    [Fact]
    public async Task Dashboard_ReturnsOnlyOrganisersEventsAndCorrectEnrolmentCount()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.dashboard.org@test.com", "Organiser");
        var (otherOrganiser, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.dashboard.other@test.com", "Organiser");
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.dashboard.part@test.com", "Participant");

        var ownEvent = await TestHelpers.CreateEventAsync(organiser, "My dashboard event");
        var otherEvent = await TestHelpers.CreateEventAsync(otherOrganiser, "Other dashboard event");
        var category = await TestHelpers.CreateCategoryAsync(organiser, ownEvent.EventId);
        var enrolment = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, enrolment.StatusCode);

        var response = await organiser.GetAsync("/api/organiser/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<DashboardItemResponse>>();
        var ownItem = Assert.Single(items!, x => x.EventId == ownEvent.EventId);
        Assert.Equal(1, ownItem.EnrolmentCount);
        Assert.DoesNotContain(items!, x => x.EventId == otherEvent.EventId);
    }

    [Fact]
    public async Task Entrants_ReturnsParticipantCategoryAndStatus_AndOrganiserCanChangeStatus()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.entrants.org@test.com", "Organiser");
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.entrants.part@test.com", "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);
        var enrolResponse = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var enrolment = (await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>())!;

        var entrantsResponse = await organiser.GetAsync($"/api/events/{ev.EventId}/entrants");
        Assert.Equal(HttpStatusCode.OK, entrantsResponse.StatusCode);
        var entrants = await entrantsResponse.Content.ReadFromJsonAsync<List<EntrantResponse>>();
        var entrant = Assert.Single(entrants!);
        Assert.Equal("p3.entrants.part", entrant.ParticipantName);
        Assert.Equal(category.Name, entrant.CategoryName);
        Assert.Equal("Pending", entrant.Status);

        var statusResponse = await organiser.PutAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/status", new StatusRequest { Status = "Confirmed" });
        Assert.Equal(HttpStatusCode.NoContent, statusResponse.StatusCode);

        var updated = await organiser.GetFromJsonAsync<List<EntrantResponse>>($"/api/events/{ev.EventId}/entrants");
        Assert.Equal("Confirmed", Assert.Single(updated!).Status);
    }

    [Fact]
    public async Task Entrants_AnotherOrganiserCannotViewEvent_ReturnsForbidden()
    {
        var (owner, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.entrants.owner@test.com", "Organiser");
        var (other, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.entrants.other@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var response = await other.GetAsync($"/api/events/{ev.EventId}/entrants");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ParticipantDetailEndpoints_ReturnOwnEnrolmentAndRaceHistory()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.history.org@test.com", "Organiser");
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.history.part@test.com", "Participant");
        var (secondParticipant, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.history.part2@test.com", "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser, "History 5K");
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        var firstEnrolmentResponse = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var firstEnrolment = (await firstEnrolmentResponse.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
        var secondEnrolmentResponse = await secondParticipant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var secondEnrolment = (await secondEnrolmentResponse.Content.ReadFromJsonAsync<EnrolmentResponse>())!;

        var firstResult = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{firstEnrolment.EnrolmentId}/results",
            new ResultRequest { FinishTime = TimeSpan.FromMinutes(31), Position = 1 });
        var secondResult = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{secondEnrolment.EnrolmentId}/results",
            new ResultRequest { FinishTime = TimeSpan.FromMinutes(34), Position = 2 });
        Assert.Equal(HttpStatusCode.Created, firstResult.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResult.StatusCode);

        var myEnrolments = await participant.GetFromJsonAsync<List<MyEnrolmentResponse>>("/api/enrolments/me/detail");
        var myEnrolment = Assert.Single(myEnrolments!);
        Assert.Equal("History 5K", myEnrolment.EventName);
        Assert.Equal(category.Name, myEnrolment.CategoryName);

        var myResults = await participant.GetFromJsonAsync<List<MyResultResponse>>("/api/results/me/detail");
        var myResult = Assert.Single(myResults!);
        Assert.Equal("History 5K", myResult.EventName);
        Assert.Equal(1, myResult.Position);
        Assert.Equal(2, myResult.TotalFinishers);
    }

    [Fact]
    public async Task ProfileEndpoint_ReturnsLoggedInUsersProfile()
    {
        var (client, user) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.profile@test.com", "Participant");

        var response = await client.GetAsync("/api/users/me/profile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal(user.UserId, profile!.UserId);
        Assert.Equal("p3.profile@test.com", profile.Email);
        Assert.Equal("Participant", profile.Role);
    }

    [Fact]
    public async Task UploadBanner_StoresReturnedBlobUrlOnEvent()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.banner.org@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser, "Banner test");

        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(image, "file", "banner.jpg");

        var response = await organiser.PostAsync($"/api/events/{ev.EventId}/banner", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        Assert.Equal(FakeBlobStorageService.BannerUrl, updated!.BannerUrl);

        var detail = await _factory.CreateClient().GetFromJsonAsync<EventDetailResponse>(
            $"/api/events/{ev.EventId}/detail");
        Assert.Equal(FakeBlobStorageService.BannerUrl, detail!.BannerUrl);
    }

    [Fact]
    public async Task UploadBanner_NonOwnerIsForbidden()
    {
        var (owner, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.banner.owner@test.com", "Organiser");
        var (other, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.banner.other@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(new byte[] { 1, 2, 3 });
        image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(image, "file", "banner.jpg");

        var response = await other.PostAsync($"/api/events/{ev.EventId}/banner", form);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task UploadProfilePicture_StoresReturnedBlobUrlOnUserProfile()
    {
        var (client, user) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.picture.participant@test.com", "Participant");

        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(image, "file", "profile.png");

        var upload = await client.PostAsync("/api/users/me/picture", form);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var profile = await upload.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal(user.UserId, profile!.UserId);
        Assert.Equal(FakeBlobStorageService.ProfileUrl, profile.ProfilePictureUrl);

        var persisted = await client.GetFromJsonAsync<UserProfileResponse>("/api/users/me/profile");
        Assert.Equal(FakeBlobStorageService.ProfileUrl, persisted!.ProfilePictureUrl);
    }

    [Fact]
    public async Task Participant_CannotAccessOrganiserDashboard_ReturnsForbidden()
    {
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(
            _factory, "p3.dashboard.blocked@test.com", "Participant");

        var response = await participant.GetAsync("/api/organiser/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

}
