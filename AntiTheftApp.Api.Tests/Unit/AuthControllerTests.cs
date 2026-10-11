using AntiTheftApp.Api.Controllers;
using AntiTheftApp.Api.Data;
using AntiTheftApp.Api.DTOs;
using AntiTheftApp.Api.Services;
using AntiTheftApp.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AntiTheftApp.Api.Tests.Unit;

public class AuthControllerTests
{
    private static AuthController CreateController(AppDbContext db) =>
        new(db, new JwtService(TestConfig.Create()));

    private static RegisterRequest NewUser(string email = "ali@example.com") =>
        new() { Email = email, Password = "StrongPass123", Pin = "1234" };

    [Fact]
    public async Task Register_NewUser_ReturnsTokenAndStoresHashedSecrets()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);

        var result = await controller.Register(NewUser());

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<AuthResponse>().Subject;
        body.Token.Should().NotBeNullOrEmpty();
        body.Email.Should().Be("ali@example.com");

        var saved = await db.Users.SingleAsync();
        saved.PasswordHash.Should().NotBe("StrongPass123");
        saved.PinHash.Should().NotBe("1234");
        BCrypt.Net.BCrypt.Verify("StrongPass123", saved.PasswordHash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("1234", saved.PinHash).Should().BeTrue();
    }

    [Fact]
    public async Task Register_NormalizesEmailToLowercaseAndTrims()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);

        await controller.Register(NewUser("  Ali@Example.COM "));

        (await db.Users.SingleAsync()).Email.Should().Be("ali@example.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);
        await controller.Register(NewUser());

        var result = await controller.Register(NewUser("ALI@example.com"));

        result.Result.Should().BeOfType<ConflictObjectResult>();
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsToken()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);
        await controller.Register(NewUser());

        var result = await controller.Login(new LoginRequest { Email = "ali@example.com", Password = "StrongPass123" });

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<AuthResponse>().Which.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);
        await controller.Register(NewUser());

        var result = await controller.Login(new LoginRequest { Email = "ali@example.com", Password = "WrongPass999" });

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsSameUnauthorizedAsWrongPassword()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);

        var result = await controller.Login(new LoginRequest { Email = "nobody@example.com", Password = "whatever123" });

        // Must not reveal whether the email exists.
        var unauthorized = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorized.Value!.ToString().Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);
        await controller.Register(NewUser());

        var result = await controller.Login(new LoginRequest { Email = "ALI@EXAMPLE.COM", Password = "StrongPass123" });

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
