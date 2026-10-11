using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AntiTheftApp.Api.DTOs;
using AntiTheftApp.Api.Models;
using FluentAssertions;
using Moq;

namespace AntiTheftApp.Api.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    // The API serializes enums as strings ("Sent"), so the test client must read them the same way.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiFactory _factory;

    public ApiIntegrationTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient client, AuthResponse auth)> RegisterAndAuthenticate(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = email, Password = "StrongPass123", Pin = "1234" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }

    [Fact]
    public async Task Devices_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Devices_WithGarbageToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.jwt");

        var response = await client.GetAsync("/api/devices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = "not-an-email", Password = "StrongPass123", Pin = "1234" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = "short@example.com", Password = "123", Pin = "1234" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        await RegisterAndAuthenticate("dup@example.com");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = "dup@example.com", Password = "StrongPass123", Pin = "1234" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await RegisterAndAuthenticate("login@example.com");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "login@example.com", Password = "WrongPass999" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullFlow_Register_AddDevice_ListDevices_TriggerAlarm()
    {
        _factory.Firebase
            .Setup(f => f.SendCommandAsync(It.IsAny<string>(), CommandType.SoundAlarm, It.IsAny<Guid>()))
            .ReturnsAsync("message-id");
        var (client, _) = await RegisterAndAuthenticate("flow@example.com");

        var register = await client.PostAsJsonAsync("/api/devices/register",
            new RegisterDeviceRequest { DeviceName = "Pixel", FcmToken = "tok", Platform = "Android" });
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var device = (await register.Content.ReadFromJsonAsync<DeviceResponse>())!;

        var list = await client.GetFromJsonAsync<List<DeviceResponse>>("/api/devices");
        list.Should().ContainSingle(d => d.Id == device.Id);

        var trigger = await client.PostAsJsonAsync($"/api/devices/{device.Id}/commands",
            new { type = "SoundAlarm" });
        trigger.StatusCode.Should().Be(HttpStatusCode.OK);
        var command = (await trigger.Content.ReadFromJsonAsync<CommandResponse>(Json))!;
        command.Status.Should().Be(CommandStatus.Sent);
    }

    [Fact]
    public async Task User_CannotTriggerCommandOnAnotherUsersDevice()
    {
        var (owner, _) = await RegisterAndAuthenticate("owner@example.com");
        var register = await owner.PostAsJsonAsync("/api/devices/register",
            new RegisterDeviceRequest { DeviceName = "Owner phone", FcmToken = "tok" });
        var device = (await register.Content.ReadFromJsonAsync<DeviceResponse>())!;

        var (intruder, _) = await RegisterAndAuthenticate("intruder@example.com");
        var response = await intruder.PostAsJsonAsync($"/api/devices/{device.Id}/commands",
            new { type = "SoundAlarm" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
