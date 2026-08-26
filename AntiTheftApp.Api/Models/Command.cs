namespace AntiTheftApp.Api.Models;

public enum CommandType
{
    SoundAlarm,
    StopAlarm,
    LocateDevice,
    LockDevice
}

public enum CommandStatus
{
    Pending,     // created, push not yet confirmed sent
    Sent,        // FCM accepted the push
    Acknowledged // device confirmed it received/actioned it (Week 3 territory)
}

/// <summary>
/// Every remote trigger is logged here — doubles as an audit trail
/// ("who triggered my alarm and when") and lets the mobile app poll
/// for missed commands if a push was dropped.
/// </summary>
public class Command
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DeviceId { get; set; }
    public Device? Device { get; set; }

    public CommandType Type { get; set; }

    public CommandStatus Status { get; set; } = CommandStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AcknowledgedAt { get; set; }
}
