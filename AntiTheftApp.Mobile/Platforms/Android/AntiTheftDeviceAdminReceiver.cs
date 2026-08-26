using Android.App.Admin;
using Android.Content;
using AntiTheftApp.Mobile.Services;

namespace AntiTheftApp.Mobile.Platforms.Android;

// A regular Device Admin (not a Device Owner). This lets the app:
//  1. Force-lock the screen remotely (DevicePolicyManager.LockNow()).
//  2. Require an explicit "Deactivate this device admin app" step in
//     Settings > Security before the app can be uninstalled — adding
//     friction for a casual thief.
//
// IMPORTANT — what this does NOT do: it cannot block a factory reset
// triggered from Settings, and it has no effect at all on a hardware
// recovery-mode wipe. Only a fully-provisioned Device Owner can restrict
// factory reset from Settings, and even that has no power over recovery
// mode. See README for the full explanation.
[BroadcastReceiver(
    Label = "AntiTheftApp Device Admin",
    Permission = global::Android.Manifest.Permission.BindDeviceAdmin)]
[IntentFilter(new[] { "android.app.action.DEVICE_ADMIN_ENABLED" })]
[MetaData("android.app.device_admin", Resource = "@xml/device_admin_policies")]
public class AntiTheftDeviceAdminReceiver : DeviceAdminReceiver
{
    public override void OnEnabled(Context context, Intent intent)
    {
        base.OnEnabled(context, intent);
        Microsoft.Maui.Storage.Preferences.Set("device_admin_active", true);
    }

    // Called when the user starts the "deactivate" flow in Settings — this is
    // the step most OEMs force before letting someone uninstall the app.
    // We can't block it (Android doesn't allow that for a plain Device Admin),
    // but we get a brief window to fire a best-effort warning ping.
    public override string? OnDisableRequested(Context context, Intent intent)
    {
        _ = TryNotifyBackendAsync("device_admin_disable_requested");
        return "Removing this will disable remote lock protection on this device.";
    }

    public override void OnDisabled(Context context, Intent intent)
    {
        base.OnDisabled(context, intent);
        Microsoft.Maui.Storage.Preferences.Set("device_admin_active", false);
        _ = TryNotifyBackendAsync("device_admin_disabled");
    }

    // Best-effort only: if the thief has already pulled network/airplane mode,
    // or the OS tears the process down before this completes, the ping never
    // lands. This is a bonus signal, not something the design depends on.
    private static async System.Threading.Tasks.Task TryNotifyBackendAsync(string eventName)
    {
        try
        {
            var settings = new AppSettingsService();
            var token = await settings.GetTokenAsync();
            var deviceId = settings.GetDeviceId();
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(deviceId)) return;

            using var client = new System.Net.Http.HttpClient
            {
                Timeout = System.TimeSpan.FromSeconds(5)
            };
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var payload = new System.Net.Http.StringContent(
                $"{{\"eventType\":\"{eventName}\"}}",
                System.Text.Encoding.UTF8,
                "application/json");

            await client.PostAsync($"{settings.ApiBaseUrl}/api/devices/{deviceId}/security-event", payload);
        }
        catch
        {
            // Swallow — this is a bonus notification, never something the
            // core lock/alarm flow should depend on or crash over.
        }
    }
}
