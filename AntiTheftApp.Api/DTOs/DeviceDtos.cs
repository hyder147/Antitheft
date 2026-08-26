using System.ComponentModel.DataAnnotations;
using AntiTheftApp.Api.Models;

namespace AntiTheftApp.Api.DTOs;

// Called by the MOBILE APP right after login, to attach its push token.
public class RegisterDeviceRequest
{
    [Required]
    public string DeviceName { get; set; } = string.Empty;

    [Required]
    public string FcmToken { get; set; } = string.Empty;

    public string Platform { get; set; } = "Android";
}

public class DeviceResponse
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public bool IsProtectionEnabled { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? LastSecurityEvent { get; set; }
    public DateTime? LastSecurityEventAt { get; set; }
}

// Called by the WEB PORTAL (or a second device) to trigger a remote alarm.
public class TriggerCommandRequest
{
    [Required]
    public CommandType Type { get; set; }
}

public class CommandResponse
{
    public Guid Id { get; set; }
    public CommandType Type { get; set; }
    public CommandStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Called by the MOBILE APP's DeviceAdminReceiver — a best-effort ping when
// someone starts removing admin protection on the phone (usually the first
// step before an uninstall). May never arrive if network is cut first.
public class SecurityEventRequest
{
    [Required]
    public string EventType { get; set; } = string.Empty;
}
