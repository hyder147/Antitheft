namespace AntiTheftApp.Mobile.Services;

public class AppSettingsService
{
    private const string TokenKey = "jwt_token";
    private const string UserIdKey = "user_id";
    private const string DevicePinHashKey = "device_pin_hash";
    private const string DeviceIdKey = "device_id";

    // Plain (non-secure) preference — survives an app process kill and a
    // device reboot, which is exactly what BootReceiver needs to check on
    // startup: "was the alarm/lock still active when the phone went down?"
    // It does NOT survive a factory reset — nothing app-side can.
    private const string DeviceLostFlagKey = "device_lost_active";

    // CHANGE THIS before building. "10.0.2.2" only works for the Android
    // Studio emulator. A physical device over USB should use "localhost"
    // AFTER running `adb reverse tcp:5000 tcp:5000`.
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";

    public async Task SaveAuthAsync(string token, string userId)
    {
        await SecureStorage.Default.SetAsync(TokenKey, token);
        await SecureStorage.Default.SetAsync(UserIdKey, userId);
    }

    public Task<string?> GetTokenAsync() => SecureStorage.Default.GetAsync(TokenKey);

    public async Task SaveDevicePinAsync(string pin)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(pin);
        await SecureStorage.Default.SetAsync(DevicePinHashKey, hash);
    }

    public async Task<bool> VerifyDevicePinAsync(string pin)
    {
        var hash = await SecureStorage.Default.GetAsync(DevicePinHashKey);
        if (string.IsNullOrEmpty(hash)) return false;
        return BCrypt.Net.BCrypt.Verify(pin, hash);
    }

    public async Task<bool> HasDevicePinAsync()
    {
        var hash = await SecureStorage.Default.GetAsync(DevicePinHashKey);
        return !string.IsNullOrEmpty(hash);
    }

    public void ClearAuth()
    {
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(UserIdKey);
    }

    public void SaveDeviceId(string deviceId) => Preferences.Default.Set(DeviceIdKey, deviceId);

    public string? GetDeviceId()
    {
        var id = Preferences.Default.Get(DeviceIdKey, string.Empty);
        return string.IsNullOrEmpty(id) ? null : id;
    }

    // Set true the moment a SoundAlarm command arrives; cleared only once the
    // correct PIN is entered. BootReceiver reads this on every device boot —
    // if it's still true, the phone was powered off/on WHILE armed, so the
    // alarm/lock screen comes straight back instead of the thief getting a
    // clean, unlocked boot.
    public void SetDeviceLostFlag(bool isLost) => Preferences.Default.Set(DeviceLostFlagKey, isLost);

    public bool GetDeviceLostFlag() => Preferences.Default.Get(DeviceLostFlagKey, false);
}
