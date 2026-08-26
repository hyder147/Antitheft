namespace AntiTheftApp.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.LoginPage), typeof(Views.LoginPage));
        Routing.RegisterRoute(nameof(Views.SetupPinPage), typeof(Views.SetupPinPage));
        Routing.RegisterRoute(nameof(Views.HomePage), typeof(Views.HomePage));
    }
}
