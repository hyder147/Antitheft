using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AntiTheftApp.Api.Data;
using AntiTheftApp.Api.DTOs;
using AntiTheftApp.Api.Models;
using AntiTheftApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AntiTheftApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // every endpoint here requires a valid JWT (from /api/auth/login)
public class DevicesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFirebaseService _firebase;

    public DevicesController(AppDbContext db, IFirebaseService firebase)
    {
        _db = db;
        _firebase = firebase;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    // Called by the MOBILE APP once, right after the user logs in on that phone.
    [HttpPost("register")]
    public async Task<ActionResult<DeviceResponse>> RegisterDevice(RegisterDeviceRequest request)
    {
        var device = new Device
        {
            UserId = CurrentUserId,
            DeviceName = request.DeviceName,
            FcmToken = request.FcmToken,
            Platform = request.Platform,
            LastSeenAt = DateTime.UtcNow
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(device));
    }

    // Called by the WEB PORTAL to show "which of my devices can I trigger?"
    [HttpGet]
    public async Task<ActionResult<List<DeviceResponse>>> GetMyDevices()
    {
        var devices = await _db.Devices
            .Where(d => d.UserId == CurrentUserId)
            .ToListAsync();

        return Ok(devices.Select(ToResponse).ToList());
    }

    // Called by the WEB PORTAL when you click "Sound Alarm".
    // This is the core end-to-end action the whole app exists for.
    [HttpPost("{deviceId:guid}/commands")]
    public async Task<ActionResult<CommandResponse>> TriggerCommand(Guid deviceId, TriggerCommandRequest request)
    {
        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == CurrentUserId);

        // Ownership check matters here: without it, any logged-in user could
        // trigger alarms on ANY device ID, not just their own.
        if (device is null)
            return NotFound(new { message = "Device not found." });

        var command = new Command
        {
            DeviceId = device.Id,
            Type = request.Type,
            Status = CommandStatus.Pending
        };
        _db.Commands.Add(command);
        await _db.SaveChangesAsync();

        try
        {
            await _firebase.SendCommandAsync(device.FcmToken, request.Type, command.Id);
            command.Status = CommandStatus.Sent;
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Common cause: FcmToken is stale (app reinstalled, token rotated).
            // Week 3 will add a proper "re-register token" flow for this.
            return StatusCode(502, new { message = "Could not deliver command to device.", detail = ex.Message });
        }

        return Ok(new CommandResponse
        {
            Id = command.Id,
            Type = command.Type,
            Status = command.Status,
            CreatedAt = command.CreatedAt
        });
    }

    // Called by the MOBILE APP's DeviceAdminReceiver as a best-effort signal
    // (e.g. "someone just started removing device admin protection on this
    // phone"). This is a bonus notification, not a guaranteed detection —
    // see AntiTheftDeviceAdminReceiver on the mobile side for why.
    [HttpPost("{deviceId:guid}/security-event")]
    public async Task<IActionResult> LogSecurityEvent(Guid deviceId, SecurityEventRequest request)
    {
        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == CurrentUserId);

        if (device is null)
            return NotFound(new { message = "Device not found." });

        device.LastSecurityEvent = request.EventType;
        device.LastSecurityEventAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok();
    }

    private static DeviceResponse ToResponse(Device d) => new()
    {
        Id = d.Id,
        DeviceName = d.DeviceName,
        Platform = d.Platform,
        IsProtectionEnabled = d.IsProtectionEnabled,
        LastSeenAt = d.LastSeenAt,
        LastSecurityEvent = d.LastSecurityEvent,
        LastSecurityEventAt = d.LastSecurityEventAt
    };
}
