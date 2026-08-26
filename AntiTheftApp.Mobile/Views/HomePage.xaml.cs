using AntiTheftApp.Mobile.Services;

namespace AntiTheftApp.Mobile.Views;

public partial class HomePage : ContentPage
{
    private readonly AppSettingsService _settings;
    private readonly IAlarmController _alarm;

    public HomePage(AppSettingsService settings, IAlarmController alarm)
    {
        InitializeComponent();
        _settings = settings;
        _alarm = alarm;
    }

    private void OnTestAlarmClicked(object sender, EventArgs e)
    {
        _alarm.StartAlarm();
    }

    private void OnStopTestAlarmClicked(object sender, EventArgs e)
    {
        _alarm.StopAlarm();
    }

    private async void OnEnableDeviceAdminClicked(object sender, EventArgs e)
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        var componentName = new global::Android.Content.ComponentName(
            context, global::Java.Lang.Class.FromType(typeof(global::AntiTheftApp.Mobile.Platforms.Android.AntiTheftDeviceAdminReceiver)));

        var devicePolicyManager = (global::Android.App.Admin.DevicePolicyManager)
            context.GetSystemService(global::Android.Content.Context.DevicePolicyService)!;

        if (devicePolicyManager.IsAdminActive(componentName))
        {
            await DisplayAlert("Already enabled", "Device Admin protection is already active on this phone.", "OK");
            return;
        }

        var intent = new global::Android.Content.Intent(global::Android.App.Admin.DevicePolicyManager.ActionAddDeviceAdmin);
        intent.PutExtra(global::Android.App.Admin.DevicePolicyManager.ExtraDeviceAdmin, componentName);
        intent.PutExtra(
            global::Android.App.Admin.DevicePolicyManager.ExtraAddExplanation,
            "Lets the app resist uninstall/force-stop and resume the lock screen after a reboot while armed.");
        intent.SetFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
#else
        await DisplayAlert("Not available", "Device Admin protection is only available on Android.", "OK");
#endif
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        _settings.ClearAuth();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
