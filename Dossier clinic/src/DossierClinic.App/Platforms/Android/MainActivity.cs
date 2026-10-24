using Android.App;
using Android.Content.PM;
using Android.OS;

namespace DossierClinic;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
        {
            e.Handled = true;
            ShowError(e.Exception.ToString());
        };
        try { base.OnCreate(savedInstanceState); }
        catch (Exception ex) { ShowError(ex.ToString()); }
    }

    void ShowError(string message)
    {
        if (message.Length > 2500) message = message[..2500];
        RunOnUiThread(() =>
        {
            var b = new AlertDialog.Builder(this);
            b!.SetTitle("Erreur");
            b.SetMessage(message);
            b.SetPositiveButton("OK", (s, e) => { });
            b.Show();
        });
    }
}
