using AntiTheftApp.Api.Controllers;
using AntiTheftApp.Api.Data;
using AntiTheftApp.Api.DTOs;
using AntiTheftApp.Api.Models;
using AntiTheftApp.Api.Services;
using AntiTheftApp.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AntiTheftApp.Api.Tests.Unit;

public class DevicesControllerTests
{
    private static (DevicesController controller, Mock<IFirebaseService> firebase) Create(AppDbContext db, Guid userId)
    {
        var firebase = new Mock<IFirebaseService>();
        var controller = new DevicesController(db, firebase.Object, NullLogger<DevicesController>.Instance).WithUser(userId);
        return (controller, firebase);
    }

    private static async Task<(User user, Device device)> SeedUserWithDevice(AppDbContext db, string email = "ali@example.com")
    {
        var user = new User { Email = email, PasswordHash = "x", PinHash = "y" };
        var device = new Device { UserId = user.Id, DeviceName = "Ali's phone", FcmToken = "token-123" };
        db.Users.Add(user);
        db.Devices.Add(device);
        await db.SaveChangesAsync();
        return (user, device);
    }

    [Fact]
    public async Task RegisterDevice_SavesDeviceForCurrentUser()
    {
        using var db = TestDb.Create();
        var userId = Guid.NewGuid();
        var (controller, _) = Create(db, userId);

        var result = await controller.RegisterDevice(new RegisterDeviceRequest
        {
            DeviceName = "Pixel 8",
            FcmToken = "fcm-abc",
            Platform = "Android"
        });

        result.Result.Should().BeOfType<OkObjectResult>();
        var saved = await db.Devices.SingleAsync();
        saved.UserId.Should().Be(userId);
        saved.DeviceName.Should().Be("Pixel 8");
        saved.FcmToken.Should().Be("fcm-abc");
    }

    [Fact]
    public async Task GetMyDevices_ReturnsOnlyCurrentUsersDevices()
    {
        using var db = TestDb.Create();
        var (alice, _) = await SeedUserWithDevice(db, "alice@example.com");
        await SeedUserWithDevice(db, "bob@example.com");
        var (controller, _) = Create(db, alice.Id);

        var result = await controller.GetMyDevices();

        var list = result.Result.Should().BeOfType<OkObjectResult>().Subject
            .Value.Should().BeAssignableTo<List<DeviceResponse>>().Subject;
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task TriggerCommand_OwnDevice_SendsPushAndMarksCommandSent()
    {
        using var db = TestDb.Create();
        var (user, device) = await SeedUserWithDevice(db);
        var (controller, firebase) = Create(db, user.Id);
        firebase.Setup(f => f.SendCommandAsync(device.FcmToken, CommandType.SoundAlarm, It.IsAny<Guid>()))
                .ReturnsAsync("message-id");

        var result = await controller.TriggerCommand(device.Id, new TriggerCommandRequest { Type = CommandType.SoundAlarm });

        var response = result.Result.Should().BeOfType<OkObjectResult>().Subject
            .Value.Should().BeOfType<CommandResponse>().Subject;
        response.Status.Should().Be(CommandStatus.Sent);
        (await db.Commands.SingleAsync()).Status.Should().Be(CommandStatus.Sent);
        firebase.Verify(f => f.SendCommandAsync("token-123", CommandType.SoundAlarm, It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task TriggerCommand_SomeoneElsesDevice_ReturnsNotFoundAndSendsNothing()
    {
        using var db = TestDb.Create();
        var (_, victimDevice) = await SeedUserWithDevice(db, "victim@example.com");
        var (attacker, _) = await SeedUserWithDevice(db, "attacker@example.com");
        var (controller, firebase) = Create(db, attacker.Id);

        var result = await controller.TriggerCommand(victimDevice.Id, new TriggerCommandRequest { Type = CommandType.SoundAlarm });

        result.Result.Should().BeOfType<NotFoundObjectResult>();
        (await db.Commands.CountAsync()).Should().Be(0);
        firebase.Verify(f => f.SendCommandAsync(It.IsAny<string>(), It.IsAny<CommandType>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task TriggerCommand_UnknownDevice_ReturnsNotFound()
    {
        using var db = TestDb.Create();
        var (controller, _) = Create(db, Guid.NewGuid());

        var result = await controller.TriggerCommand(Guid.NewGuid(), new TriggerCommandRequest { Type = CommandType.LockDevice });

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task TriggerCommand_WhenFirebaseFails_Returns502AndCommandStaysPending()
    {
        using var db = TestDb.Create();
        var (user, device) = await SeedUserWithDevice(db);
        var (controller, firebase) = Create(db, user.Id);
        firebase.Setup(f => f.SendCommandAsync(It.IsAny<string>(), It.IsAny<CommandType>(), It.IsAny<Guid>()))
                .ThrowsAsync(new InvalidOperationException("FCM token expired"));

        var result = await controller.TriggerCommand(device.Id, new TriggerCommandRequest { Type = CommandType.SoundAlarm });

        var obj = result.Result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(502);
        (await db.Commands.SingleAsync()).Status.Should().Be(CommandStatus.Pending);
    }

    [Fact]
    public async Task TriggerCommand_WhenFirebaseFails_DoesNotLeakExceptionDetails()
    {
        using var db = TestDb.Create();
        var (user, device) = await SeedUserWithDevice(db);
        var (controller, firebase) = Create(db, user.Id);
        firebase.Setup(f => f.SendCommandAsync(It.IsAny<string>(), It.IsAny<CommandType>(), It.IsAny<Guid>()))
                .ThrowsAsync(new InvalidOperationException("secret-internal-detail"));

        var result = await controller.TriggerCommand(device.Id, new TriggerCommandRequest { Type = CommandType.SoundAlarm });

        var obj = result.Result.Should().BeOfType<ObjectResult>().Subject;
        obj.Value!.ToString().Should().NotContain("secret-internal-detail");
    }

    [Fact]
    public async Task LogSecurityEvent_OwnDevice_UpdatesDevice()
    {
        using var db = TestDb.Create();
        var (user, device) = await SeedUserWithDevice(db);
        var (controller, _) = Create(db, user.Id);

        var result = await controller.LogSecurityEvent(device.Id, new SecurityEventRequest { EventType = "AdminDisableRequested" });

        result.Should().BeOfType<OkResult>();
        var saved = await db.Devices.SingleAsync();
        saved.LastSecurityEvent.Should().Be("AdminDisableRequested");
        saved.LastSecurityEventAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LogSecurityEvent_SomeoneElsesDevice_ReturnsNotFound()
    {
        using var db = TestDb.Create();
        var (_, victimDevice) = await SeedUserWithDevice(db, "victim@example.com");
        var (controller, _) = Create(db, Guid.NewGuid());

        var result = await controller.LogSecurityEvent(victimDevice.Id, new SecurityEventRequest { EventType = "x" });

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
