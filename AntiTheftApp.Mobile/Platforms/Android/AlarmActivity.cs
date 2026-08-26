using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AntiTheftApp.Mobile.Services;

namespace AntiTheftApp.Mobile.Platforms.Android;

[Activity(
    Exported = false,
    ShowWhenLocked = true,
    TurnScreenOn = true,
    ExcludeFromRecents = true,
    LaunchMode = global::Android.Content.PM.LaunchMode.SingleTask,
    Theme = "@style/Maui.SplashTheme")]
public class AlarmActivity : Activity
{
    public const string ExtraSkipGracePeriod = "skip_grace_period";

    private const int GracePeriodSeconds = 120;

    private CountDownTimer? _countdownTimer;
    private TextView? _countdownLabel;
    private bool _alarmStarted;

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        if (keyCode == Keycode.VolumeUp || keyCode == Keycode.VolumeDown || keyCode == Keycode.VolumeMute)
            return true; // block volume changes while the alarm screen is showing

        return base.OnKeyDown(keyCode, e);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_alarm);

        _countdownLabel = FindViewById<TextView>(Resource.Id.countdownLabel);
        var pinInput = FindViewById<EditText>(Resource.Id.pinInput);

        // Fixed CS0234: Added global:: prefix to resolve Android namespace conflict
        var stopButton = FindViewById<global::Android.Widget.Button>(Resource.Id.stopButton);

        stopButton!.Click += async (_, _) => await OnStopClickedAsync(pinInput!.Text ?? string.Empty);

        if (Intent?.GetBooleanExtra(ExtraSkipGracePeriod, false) == true)
        {
            // Resuming after a reboot: the phone was already armed (past or
            // mid grace-period) when it powered off — don't hand the thief
            // a fresh 120s window, go straight to the siren.
            _countdownLabel!.Text = "Alarm active";
            OnGracePeriodExpired();
        }
        else
        {
            StartGracePeriodCountdown();
        }

        EnterImmersiveMode();
        TryStartLockTask();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus) EnterImmersiveMode();
    }

    // Hides status bar / nav bar so it's harder to swipe down to quick settings or reach home/back gestures.
    private void EnterImmersiveMode()
    {
        Window!.DecorView.SystemUiVisibility = (StatusBarVisibility)(
            SystemUiFlags.ImmersiveSticky |
            SystemUiFlags.HideNavigation |
            SystemUiFlags.Fullscreen |
            SystemUiFlags.LayoutStable |
            SystemUiFlags.LayoutHideNavigation |
            SystemUiFlags.LayoutFullscreen);
    }

    // Screen Pinning: keeps this Activity locked on screen, blocking Home/Recents while pinned.
    // Note: Android reserves the physical power button at the OS level — no app, including this
    // one, can prevent a power-off via the hardware button. Screen pinning only blocks in-app
    // navigation (home/recents/back), not the power button.
    private void TryStartLockTask()
    {
        try
        {
            StartLockTask();
        }
        catch
        {
            // Some OEM/Android versions restrict programmatic lock task without additional
            // setup (device owner / screen pinning enabled in Settings). Fails silently so the
            // alarm screen still works even if pinning isn't available on this device.
        }
    }

    // Consume the back button entirely — do NOT call base.OnBackPressed(), which would close the activity.
    public override void OnBackPressed()
    {
    }

    private void StartGracePeriodCountdown()
    {
        _countdownTimer = new GracePeriodCountdown(this, GracePeriodSeconds * 1000L, 1000L);
        _countdownTimer.Start();
    }

    private void OnGracePeriodExpired()
    {
        if (_alarmStarted) return;
        _alarmStarted = true;
        new AlarmControllerAndroid().StartAlarm();
    }

    private async System.Threading.Tasks.Task OnStopClickedAsync(string enteredPin)
    {
        var settings = new AppSettingsService();
        var isCorrect = await settings.VerifyDevicePinAsync(enteredPin);

        if (isCorrect)
        {
            _countdownTimer?.Cancel();
            settings.SetDeviceLostFlag(false);
            new AlarmControllerAndroid().StopAlarm();
            try { StopLockTask(); } catch { /* wasn't pinned, ignore */ }
            Finish();
        }
        else
        {
            Toast.MakeText(this, "Incorrect PIN", ToastLength.Short)?.Show();
        }
    }

    protected override void OnDestroy()
    {
        _countdownTimer?.Cancel();
        base.OnDestroy();
    }

    private class GracePeriodCountdown : CountDownTimer
    {
        private readonly AlarmActivity _owner;

        public GracePeriodCountdown(AlarmActivity owner, long millisInFuture, long countDownInterval)
            : base(millisInFuture, countDownInterval)
        {
            _owner = owner;
        }

        public override void OnTick(long millisUntilFinished)
        {
            var secondsLeft = millisUntilFinished / 1000;
            _owner.RunOnUiThread(() =>
                _owner._countdownLabel!.Text = $"Alarm sounds in {secondsLeft / 60}:{secondsLeft % 60:D2}");
        }

        public override void OnFinish()
        {
            _owner.RunOnUiThread(() => _owner._countdownLabel!.Text = "Alarm active");
            _owner.OnGracePeriodExpired();
        }
    }
}
