using Android.Content;
using AntiTheftApp.Mobile.Services;

namespace AntiTheftApp.Mobile.Platforms.Android;

public class AlarmControllerAndroid : IAlarmController
{
    public void StartAlarm()
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(AlarmForegroundService));
        intent.SetAction(AlarmForegroundService.ActionStart);

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public void StopAlarm()
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(AlarmForegroundService));
        intent.SetAction(AlarmForegroundService.ActionStop);
        context.StartService(intent);
    }
}
