using AntiTheftApp.Mobile.Services;

namespace AntiTheftApp.Mobile.Views;

public partial class SetupPinPage : ContentPage
{
    private readonly AppSettingsService _settings;

    public SetupPinPage(AppSettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (string.IsNullOrWhiteSpace(PinEntry.Text) || PinEntry.Text.Length < 4)
        {
            ShowError("PIN must be at least 4 digits.");
            return;
        }
        if (PinEntry.Text != ConfirmPinEntry.Text)
        {
            ShowError("PINs don't match.");
            return;
        }

        await _settings.SaveDevicePinAsync(PinEntry.Text);
        await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
