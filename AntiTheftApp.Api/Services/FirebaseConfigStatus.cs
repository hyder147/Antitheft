namespace AntiTheftApp.Api.Services;

/// <summary>
/// Set once at startup in Program.cs based on whether a real Firebase
/// service account file was found. Injected into FirebaseService so it
/// can fail with a clear, actionable message instead of a raw NRE when
/// someone (understandably) hasn't set up Firebase yet.
/// </summary>
public class FirebaseConfigStatus
{
    public bool IsConfigured { get; }

    public FirebaseConfigStatus(bool isConfigured)
    {
        IsConfigured = isConfigured;
    }
}