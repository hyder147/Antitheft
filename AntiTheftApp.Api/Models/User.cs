namespace AntiTheftApp.Api.Models;

/// <summary>
/// The account owner. PasswordHash is used to log into the web portal /
/// a second device. PinHash is the shorter PIN entered ON the protected
/// device itself to silence an alarm once it starts.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PinHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
