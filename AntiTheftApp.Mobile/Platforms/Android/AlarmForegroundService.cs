using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;

namespace AntiTheftApp.Mobile.Platforms.Android;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public class AlarmForegroundService : Service
{
    public const string ActionStart = "com.antitheftapp.ACTION_START_ALARM";
    public const string ActionStop = "com.antitheftapp.ACTION_STOP_ALARM";

    private const string ChannelId = "antitheftapp_alarm_channel";
    private const int NotificationId = 1001;

    private MediaPlayer? _player;
    private Vibrator? _vibrator;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ActionStop)
        {
            StopAlarmSound();
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        StartForeground(NotificationId, BuildNotification());
        PlayAlarmSound();

        return StartCommandResult.Sticky;
    }

    private void PlayAlarmSound()
    {
        if (_player is not null) return;

        // Force the ALARM stream to max volume — this is the actual fix for "sound doesn't
        // play" on phones where the user's Alarm volume slider is turned down, and it also
        // makes the siren as loud/intense as the device allows.
        var audioManager = (AudioManager)GetSystemService(AudioService)!;
        var maxAlarmVolume = audioManager.GetStreamMaxVolume(global::Android.Media.Stream.Alarm);
        audioManager.SetStreamVolume(global::Android.Media.Stream.Alarm, maxAlarmVolume, VolumeNotificationFlags.PlaySound);

        var alarmUri = GetSirenUri();

        var attributes = new AudioAttributes.Builder()!
            .SetUsage(AudioUsageKind.Alarm)!
            .SetContentType(AudioContentType.Sonification)!
            .Build();

        _player = new MediaPlayer();
        _player.SetAudioAttributes(attributes!);
        _player.SetDataSource(this, alarmUri!);
        _player.SetVolume(1.0f, 1.0f);

        // Fixed: C# binding property syntax
        _player.Looping = true;

        _player.Prepare();
        _player.Start();

        StartVibration();
    }

    // Looks for a bundled custom siren at Resources/Raw/siren.(mp3|wav|ogg) via GetIdentifier,
    // so this compiles fine whether or not the file has been added yet. Falls back to the
    // device's default alarm ringtone if no custom siren is bundled.
    private global::Android.Net.Uri GetSirenUri()
    {
        var resId = Resources!.GetIdentifier("siren", "raw", PackageName);
        if (resId != 0)
        {
            return global::Android.Net.Uri.Parse($"android.resource://{PackageName}/{resId}")!;
        }

        return RingtoneManager.GetActualDefaultRingtoneUri(this, RingtoneType.Alarm)
               ?? RingtoneManager.GetDefaultUri(RingtoneType.Alarm)!;
    }

    private void StartVibration()
    {
        _vibrator = (Vibrator)GetSystemService(VibratorService)!;
        var pattern = new long[] { 0, 500, 200, 500, 200 };
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            _vibrator.Vibrate(VibrationEffect.CreateWaveform(pattern, 0));
        }
        else
        {
#pragma warning disable CA1422
            _vibrator.Vibrate(pattern, 0);
#pragma warning restore CA1422
        }
    }

    private void StopAlarmSound()
    {
        if (_player is not null)
        {
            _player.Stop();
            _player.Release();
            _player = null;
        }

        _vibrator?.Cancel();
        _vibrator = null;
    }

    private Notification BuildNotification()
    {
        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Anti-Theft Alarm", NotificationImportance.High)
            {
                Description = "Shown while the anti-theft alarm is sounding"
            };
            manager.CreateNotificationChannel(channel);
        }

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Alarm active")
            .SetContentText("Enter your PIN on this device to stop the alarm.")
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogAlert)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityMax)
            .Build();
    }

    public override void OnDestroy()
    {
        StopAlarmSound();
        base.OnDestroy();
    }
}
