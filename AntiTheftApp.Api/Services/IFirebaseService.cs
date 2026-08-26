using AntiTheftApp.Api.Models;

namespace AntiTheftApp.Api.Services;

public interface IFirebaseService
{
    /// <summary>
    /// Sends a high-priority data push to a single device by its FCM token.
    /// This is the mechanism that wakes the mobile app even if it's
    /// backgrounded or killed, so it can start the foreground alarm service.
    /// </summary>
    Task<string> SendCommandAsync(string fcmToken, CommandType type, Guid commandId);
}
