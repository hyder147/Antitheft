namespace AntiTheftApp.Api.Models;

/// <summary>
/// A phone/tablet the user has installed the protection app on.
/// FcmToken is what lets the backend push a "sound alarm" command
/// to this exact device, even if the app is backgrounded or killed.
/// </summary>
public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string DeviceName { get; set; } = string.Empty; // e.g. "Ali's Galaxy S24"

    public string FcmToken { get; set; } = string.Empty;

    public string Platform { get; set; } = "Android";

    public bool IsProtectionEnabled { get; set; } = true;

    public DateTime? LastSeenAt { get; set; }

    // Best-effort signal from the phone's DeviceAdminReceiver — e.g. someone
    // tried to deactivate admin protection (usually the first step before an
    // uninstall). Not guaranteed to arrive (see AntiTheftDeviceAdminReceiver).
    public string? LastSecurityEvent { get; set; }
    public DateTime? LastSecurityEventAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Command> Commands { get; set; } = new List<Command>();
}
