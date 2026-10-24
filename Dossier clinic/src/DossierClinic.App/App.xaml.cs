using DossierClinic.Services;
using DossierClinic.Views;

namespace DossierClinic;

public partial class App : Application
{
    readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        AppPrefs.ApplyTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(_services.GetRequiredService<SplashPage>());

    /// <summary>Called by the splash screen once the local database is ready.</summary>
    public void ShowMainShell()
    {
        if (Windows.Count > 0) Windows[0].Page = _services.GetRequiredService<AppShell>();
    }
}
