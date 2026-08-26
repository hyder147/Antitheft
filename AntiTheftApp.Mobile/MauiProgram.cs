using Microsoft.Extensions.Logging;
using AntiTheftApp.Mobile.Services;

namespace AntiTheftApp.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Shared services
        builder.Services.AddSingleton<AppSettingsService>();
        builder.Services.AddSingleton<ApiClient>();

#if ANDROID
        builder.Services.AddSingleton<IAlarmController, AntiTheftApp.Mobile.Platforms.Android.AlarmControllerAndroid>();
#endif

        builder.Services.AddTransient<Views.WelcomePage>();
        builder.Services.AddTransient<Views.LoginPage>();
        builder.Services.AddTransient<Views.SetupPinPage>();
        builder.Services.AddTransient<Views.HomePage>();

#if DEBUG
        // Commented out to resolve CS1061 AddDebug extension error
        // builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}