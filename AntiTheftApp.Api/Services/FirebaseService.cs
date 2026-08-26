using AntiTheftApp.Api.Models;
using FirebaseAdmin.Messaging;

namespace AntiTheftApp.Api.Services;

public class FirebaseService : IFirebaseService
{
    private readonly FirebaseConfigStatus _configStatus;

    public FirebaseService(FirebaseConfigStatus configStatus)
    {
        _configStatus = configStatus;
    }

    public async Task<string> SendCommandAsync(string fcmToken, CommandType type, Guid commandId)
    {
        if (!_configStatus.IsConfigured)
        {
            // This is the #1 expected failure during local dev — surface it
            // clearly instead of letting FirebaseMessaging.DefaultInstance
            // throw a raw NullReferenceException when no FirebaseApp exists.
            throw new InvalidOperationException(
                "Firebase is not configured yet. Add a real firebase-service-account.json " +
                "file to AntiTheftApp.Api/ (see README, Step 2) to enable push delivery.");
        }

        // Data-only message (not "notification") so the app can react in the
        // background without Android auto-showing a tray notification first —
        // your foreground service decides what happens (start the alarm, etc).
        var message = new Message
        {
            Token = fcmToken,
            Data = new Dictionary<string, string>
            {
                { "commandId", commandId.ToString() },
                { "commandType", type.ToString() }
            },
            Android = new AndroidConfig
            {
                Priority = Priority.High // required so the OS delivers it immediately, not batched
            }
        };

        // Throws FirebaseMessagingException if the token is invalid/expired —
        // let that bubble up to the controller so it can be handled there.
        return await FirebaseMessaging.DefaultInstance.SendAsync(message);
    }
}