using System.Net.Http.Json;
using System.Text.Json;

namespace AntiTheftApp.Mobile.Services;

public record AuthResult(string Token, DateTime ExpiresAt, string UserId, string Email);
public record DeviceResult(string Id, string DeviceName, string Platform, bool IsProtectionEnabled);

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AppSettingsService _settings;

    public ApiClient(AppSettingsService settings)
    {
        _settings = settings;
        _http = new HttpClient { BaseAddress = new Uri(_settings.ApiBaseUrl) };
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string pin)
    {
        var response = await _http.PostAsJsonAsync("/api/Auth/register", new { email, password, pin });
        return await HandleAuthResponseAsync(response);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("/api/Auth/login", new { email, password });
        return await HandleAuthResponseAsync(response);
    }

    public async Task<DeviceResult> RegisterDeviceAsync(string fcmToken, string deviceName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Devices/register")
        {
            Content = JsonContent.Create(new { deviceName, fcmToken, platform = "Android" })
        };
        await AttachAuthAsync(request);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Could not register this device with the server ({(int)response.StatusCode}).");

        var result = await response.Content.ReadFromJsonAsync<DeviceResult>();
        return result ?? throw new Exception("Empty response from server.");
    }

    public async Task<List<DeviceResult>> GetDevicesAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Devices");
        await AttachAuthAsync(request);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<DeviceResult>>() ?? new();
    }

    private async Task AttachAuthAsync(HttpRequestMessage request)
    {
        var token = await _settings.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<AuthResult> HandleAuthResponseAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            string message;
            try
            {
                using var doc = JsonDocument.Parse(body);
                message = doc.RootElement.TryGetProperty("message", out var m) ? (m.GetString() ?? body) : body;
            }
            catch { message = body; }

            throw new Exception(message);
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        return result ?? throw new Exception("Empty response from server.");
    }
}
