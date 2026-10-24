using DossierClinic.Services;

namespace DossierClinic.Views;

public partial class SplashPage : ContentPage
{
    readonly ClinicService _db;
    Task? _init;

    public SplashPage(ClinicService db)
    {
        InitializeComponent();
        _db = db;
        Shell.SetNavBarIsVisible(this, false);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _init ??= _db.InitAsync();
    }

    async void OnStart(object? sender, EventArgs e)
    {
        StartButton.IsEnabled = false;
        await (_init ??= _db.InitAsync());
        ((App)Application.Current!).ShowMainShell();
    }
}
