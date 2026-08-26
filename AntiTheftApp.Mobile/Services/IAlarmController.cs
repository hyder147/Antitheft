namespace AntiTheftApp.Mobile.Services;

/// <summary>
/// Platform-agnostic contract for starting/stopping the alarm. The real
/// implementation lives in Platforms/Android/AlarmControllerAndroid.cs,
/// since starting a foreground Service is an Android-specific concept.
/// Registered in MauiProgram.cs behind an #if ANDROID block.
/// </summary>
public interface IAlarmController
{
    void StartAlarm();
    void StopAlarm();
}