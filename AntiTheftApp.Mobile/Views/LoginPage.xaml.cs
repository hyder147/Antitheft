using AntiTheftApp.Mobile.Services;

namespace AntiTheftApp.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly AppSettingsService _settings;

    public LoginPage(ApiClient api, AppSettingsService settings)
    {
        InitializeComponent();
        _api = api;
        _settings = settings;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
        => await AuthenticateAsync(isRegister: false);

    private async void OnRegisterClicked(object sender, EventArgs e)
        => await AuthenticateAsync(isRegister: true);

    private async Task AuthenticateAsync(bool isRegister)
    {
        ErrorLabel.IsVisible = false;

        if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            ShowError("Enter both email and password.");
            return;
        }

        Spinner.IsVisible = true;
        Spinner.IsRunning = true;

        try
        {
            AuthResult result;
            if (isRegister)
            {
                if (string.IsNullOrWhiteSpace(PinEntry.Text))
                {
                    ShowError("Choose an account PIN to register.");
                    return;
                }
                result = await _api.RegisterAsync(EmailEntry.Text.Trim(), PasswordEntry.Text, PinEntry.Text);
            }
            else
            {
                result = await _api.LoginAsync(EmailEntry.Text.Trim(), PasswordEntry.Text);
            }

            await _settings.SaveAuthAsync(result.Token, result.UserId);

            var fcmToken = await GetFcmTokenWithRetryAsync();
            var device = await _api.RegisterDeviceAsync(fcmToken, DeviceInfo.Current.Name);
            _settings.SaveDeviceId(device.Id);

            await Shell.Current.GoToAsync(nameof(SetupPinPage));
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            Spinner.IsVisible = false;
            Spinner.IsRunning = false;
        }
    }

    private static async Task<string> GetFcmTokenWithRetryAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var token = Preferences.Get("fcm_token", string.Empty);
            if (!string.IsNullOrEmpty(token)) return token;
            await Task.Delay(500);
        }

        return "pending-token";
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
