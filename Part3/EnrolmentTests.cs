using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

public class EnrolmentTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EnrolmentTests(RaceDayApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_EnrolmentIsRecordedCorrectly()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "enrol.org@test.com", "Organiser");
        var (participant, participantUser) = await TestHelpers.CreateLoggedInClientAsync(_factory, "enrol.part@test.com", "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        var response = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(participantUser.UserId, enrolment!.ParticipantId);
        Assert.Equal(category.CategoryId, enrolment.CategoryId);

        // Participant sees it in their own list
        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/me");
        Assert.Contains(mine!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Participant_EnrollingTwiceInSameCategory_Returns409()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "dup.org@test.com", "Organiser");
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "dup.part@test.com", "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var second = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_NonExistentCategory_Returns404()
    {
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "ghostcat@test.com", "Participant");

        var response = await participant.PostAsync("/api/categories/999999/enrolments", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolAsParticipant_Returns403()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "orgenrol@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);

        var response = await organiser.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "view.org@test.com", "Organiser");
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "view.part@test.com", "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);
        await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        var response = await organiser.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Single(list!);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsForAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "viewowner@test.com", "Organiser");
        var (other, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "viewother@test.com", "Organiser");
        var ev = await TestHelpers.CreateEventAsync(owner);

        var response = await other.GetAsync($"/api/events/{ev.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Results ---

    [Fact]
    public async Task Organiser_CanCaptureResult_ParticipantCanViewIt()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "res.org@test.com", "Organiser");
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "res.part@test.com", "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);
        var enrolResponse = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var enrolment = (await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>())!;

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results",
            new ResultRequest { FinishTime = TimeSpan.FromMinutes(52), Position = 3 });

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var mine = await participant.GetFromJsonAsync<List<ResultResponse>>("/api/results/me");
        Assert.Single(mine!);
        Assert.Equal(3, mine![0].Position);
    }

    [Fact]
    public async Task Participant_CannotSelfReportResult_Returns403()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "self.org@test.com", "Organiser");
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "self.part@test.com", "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);
        var enrolResponse = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var enrolment = (await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>())!;

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results",
            new ResultRequest { FinishTime = TimeSpan.FromMinutes(30), Position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "twice.org@test.com", "Organiser");
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "twice.part@test.com", "Participant");
        var ev = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser, ev.EventId);
        var enrolResponse = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var enrolment = (await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
        var body = new ResultRequest { FinishTime = TimeSpan.FromMinutes(45), Position = 2 };

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results", body);
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "resowner@test.com", "Organiser");
        var (other, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "resother@test.com", "Organiser");
        var (participant, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "respart2@test.com", "Participant");
        var ev = await TestHelpers.CreateEventAsync(owner);
        var category = await TestHelpers.CreateCategoryAsync(owner, ev.EventId);
        var enrolResponse = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);
        var enrolment = (await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>())!;

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/results",
            new ResultRequest { FinishTime = TimeSpan.FromMinutes(40), Position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
