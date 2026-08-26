using Android.App;
using Android.Content;
using AndroidX.Core.App;
using AntiTheftApp.Mobile.Services;
using Firebase.Messaging;

namespace AntiTheftApp.Mobile.Platforms.Android;

[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class FcmService : FirebaseMessagingService
{
    private const string AlarmChannelId = "antitheftapp_incoming_channel";

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        if (!message.Data.TryGetValue("commandType", out var commandType))
            return;

        switch (commandType)
        {
            case "SoundAlarm":
                // Marked BEFORE showing the notification: if the phone is
                // powered off and back on while this is still true,
                // BootReceiver brings the lock/alarm screen straight back
                // instead of the thief getting a clean unlocked boot.
                new AppSettingsService().SetDeviceLostFlag(true);
                ShowFullScreenAlarmNotification();
                break;

            case "StopAlarm":
                new AppSettingsService().SetDeviceLostFlag(false);
                var stopIntent = new Intent(this, typeof(AlarmForegroundService));
                stopIntent.SetAction(AlarmForegroundService.ActionStop);
                StartService(stopIntent);
                break;
        }
    }

    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        Microsoft.Maui.Storage.Preferences.Set("fcm_token", token);
    }

    private void ShowFullScreenAlarmNotification()
    {
        var alarmActivityIntent = new Intent(this, typeof(AlarmActivity));
        alarmActivityIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

        var pendingIntent = PendingIntent.GetActivity(
            this, 0, alarmActivityIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(AlarmChannelId, "Incoming Alarm Trigger", NotificationImportance.High);
            manager.CreateNotificationChannel(channel);
        }

        var notification = new NotificationCompat.Builder(this, AlarmChannelId)
            .SetContentTitle("Device alarm triggered")
            .SetContentText("Tap to open")
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogAlert)
            .SetPriority(NotificationCompat.PriorityMax)
            .SetCategory(NotificationCompat.CategoryAlarm)
            .SetFullScreenIntent(pendingIntent, true)
            .SetAutoCancel(true)
            .Build();

        manager.Notify(9999, notification);
        StartActivity(alarmActivityIntent);
    }
}
