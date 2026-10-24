namespace DossierClinic.Services;

/// <summary>Per-device preferences (not synchronised).</summary>
public static class AppPrefs
{
    public static bool DarkTheme { get => Preferences.Get(nameof(DarkTheme), false); set => Preferences.Set(nameof(DarkTheme), value); }
    public static string ServerUrl { get => Preferences.Get(nameof(ServerUrl), ""); set => Preferences.Set(nameof(ServerUrl), value.Trim()); }
    public static string ApiKey { get => Preferences.Get(nameof(ApiKey), ""); set => Preferences.Set(nameof(ApiKey), value.Trim()); }
    public static DateTime LastSyncUtc
    {
        get => DateTime.TryParse(Preferences.Get(nameof(LastSyncUtc), ""), null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : DateTime.MinValue;
        set => Preferences.Set(nameof(LastSyncUtc), value.ToString("O"));
    }
    public static void ApplyTheme()
    {
        if (Application.Current is { } a) a.UserAppTheme = DarkTheme ? AppTheme.Dark : AppTheme.Light;
    }
}
