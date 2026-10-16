using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public AuthTests(RaceDayApiFactory factory) => _factory = factory;

    private static RegisterRequest NewUser(string email, string role = "Participant") => new()
    {
        Name = "Test User",
        Email = email,
        Password = "Passw0rd!",
        Role = role
    };

    // --- Registration ---

    [Fact]
    public async Task Register_ValidUser_Returns201()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("reg.ok@test.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("reg.ok@test.com", body!.Email);
        Assert.Equal("Participant", body.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", NewUser("dupe@test.com"));

        var second = await client.PostAsJsonAsync("/api/auth/register", NewUser("dupe@test.com"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("badrole@test.com", "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ResponseNeverExposesPassword()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser("nopw@test.com"));
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Passw0rd!", raw);
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
    }

    // --- Login ---

    [Fact]
    public async Task Login_ValidCredentials_Returns200AndSessionCookie()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", NewUser("login.ok@test.com"));

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "login.ok@test.com", Password = "Passw0rd!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers, h => h.Key == "Set-Cookie"
            && h.Value.Any(v => v.StartsWith("RaceDay.Session")));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", NewUser("login.bad@test.com"));

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "login.bad@test.com", Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "ghost@test.com", Password = "whatever" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Session persistence & protected areas ---

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_AfterLogin_SessionKeepsUserAuthenticated()
    {
        var (client, user) = await TestHelpers.CreateLoggedInClientAsync(_factory, "session@test.com", "Participant");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(user.UserId, me!.UserId);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var (client, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "logout@test.com", "Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var afterLogout = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ChangesNameAndEmail()
    {
        var (client, _) = await TestHelpers.CreateLoggedInClientAsync(_factory, "profile@test.com", "Participant");

        var response = await client.PutAsJsonAsync("/api/users/me",
            new UpdateProfileRequest { Name = "New Name", Email = "profile.new@test.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal("profile.new@test.com", updated.Email);
    }
}
