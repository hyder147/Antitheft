using Android.Content;
using AntiTheftApp.Mobile.Services;

namespace AntiTheftApp.Mobile.Platforms.Android;

// Fires when Android finishes booting. If the alarm/lock was still active
// the moment the phone powered off (owner never entered the correct PIN),
// we bring the full-screen alarm + PIN lock straight back — so a thief
// can't clear the lock screen just by power-cycling the phone.
//
// LIMITS, stated plainly:
//  - This only runs if the app has been opened at least once since install
//    (Android 12+ keeps a freshly-installed, never-opened app in a
//    "stopped state" that blocks BOOT_COMPLETED). Since the owner sets the
//    app up before the phone is ever lost, this is normally already satisfied.
//  - Some OEMs (Xiaomi/MIUI, Huawei, Oppo, etc.) aggressively kill
//    auto-start receivers unless the user manually allows "autostart" for
//    the app in that OEM's battery settings. No code-only fix exists for
//    this — it needs a one-time manual toggle by the owner, ideally
//    prompted during setup.
//  - This does NOT survive a factory reset. A reset wipes the app itself,
//    so there is nothing left on the device to run this receiver at all.
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[]
{
    Intent.ActionBootCompleted,
    "android.intent.action.QUICKBOOT_POWERON" // fired by some OEMs instead of/alongside ActionBootCompleted
})]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != Intent.ActionBootCompleted &&
            intent?.Action != "android.intent.action.QUICKBOOT_POWERON")
            return;

        var settings = new AppSettingsService();
        if (!settings.GetDeviceLostFlag())
            return; // owner had already entered the PIN before the phone was powered off — nothing to resume

        var alarmActivityIntent = new Intent(context, typeof(AlarmActivity));
        alarmActivityIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
        alarmActivityIntent.PutExtra(AlarmActivity.ExtraSkipGracePeriod, true);
        context?.StartActivity(alarmActivityIntent);
    }
}
