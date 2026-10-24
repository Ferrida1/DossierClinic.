using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DossierClinic.Services;
using DossierClinic.Shared;

namespace DossierClinic.ViewModels;

public partial class InvoicesViewModel : ObservableObject
{
    readonly ClinicService _db;
    public InvoicesViewModel(ClinicService db) => _db = db;

    public string[] Filters { get; } = { "Toutes", "À encaisser", "Soldées" };
    public ObservableCollection<InvoiceRow> Items { get; } = new();
    [ObservableProperty] string selectedFilter = "Toutes";
    [ObservableProperty] string collectedText = Fmt.Fcfa(0);
    [ObservableProperty] string outstandingText = Fmt.Fcfa(0);

    public async Task LoadAsync()
    {
        var s = await _db.GetInvoicesAsync(SelectedFilter);
        Items.Clear(); foreach (var r in s.Rows) Items.Add(r);
        CollectedText = Fmt.Fcfa(s.Collected); OutstandingText = Fmt.Fcfa(s.Outstanding);
    }

    [RelayCommand] async Task SetFilter(string filter) { SelectedFilter = filter; await LoadAsync(); }
    [RelayCommand] Task Open(InvoiceRow row) => Nav.GoAsync(Nav.Routes.Invoice, ("id", row.Id));
}

[QueryProperty("Id", "id")]
public partial class InvoiceDetailViewModel : ObservableObject
{
    readonly ClinicService _db;
    Invoice? _invoice;
    public InvoiceDetailViewModel(ClinicService db) => _db = db;

    public ObservableCollection<LineRow> Lines { get; } = new();
    public ObservableCollection<LineRow> Payments { get; } = new();
    [ObservableProperty] string id = "";
    [ObservableProperty] string dateText = "";
    [ObservableProperty] string title = "";
    [ObservableProperty] StatusStyle statusStyle = StatusStyle.For(InvoiceStatus.Impayee);
    [ObservableProperty] string patientLine = "";
    [ObservableProperty] string totalText = "";
    [ObservableProperty] string remainingText = "";
    [ObservableProperty] bool canPay;

    public async Task LoadAsync()
    {
        if (await _db.GetInvoiceAsync(Id) is not { } inv) return;
        _invoice = inv;
        DateText = Fmt.Day(inv.Date);
        Title = $"Facture {inv.Number}";
        StatusStyle = StatusStyle.For(inv.Status);
        PatientLine = $"{inv.Patient?.FullName} · {inv.Patient?.Number}";
        Lines.Clear(); foreach (var l in inv.Lines) Lines.Add(new LineRow(l.Label, Fmt.Fcfa(l.Amount)));
        Payments.Clear(); foreach (var p in inv.Payments) Payments.Add(new LineRow($"Payé le {Fmt.Day(p.Date)}", Fmt.Fcfa(p.Amount)));
        TotalText = Fmt.Fcfa(inv.Total); RemainingText = Fmt.Fcfa(inv.Remaining);
        CanPay = inv.Remaining > 0;
    }

    [RelayCommand] Task Back() => Nav.BackAsync();
    [RelayCommand] Task Pay() => Nav.GoAsync(Nav.Routes.Payment, ("invoiceId", Id));

    [RelayCommand]
    async Task ShareInvoice()
    {
        if (_invoice is null) return;
        var s = await _db.GetSettingsAsync();
        var sb = new StringBuilder();
        sb.AppendLine($"{s.ClinicName} — FACTURE {_invoice.Number}");
        sb.AppendLine($"Date : {Fmt.Day(_invoice.Date)}");
        sb.AppendLine($"Patient : {_invoice.Patient?.FullName}");
        sb.AppendLine();
        foreach (var l in _invoice.Lines) sb.AppendLine($"{l.Label} : {Fmt.Fcfa(l.Amount)}");
        sb.AppendLine($"Total : {Fmt.Fcfa(_invoice.Total)}");
        foreach (var p in _invoice.Payments) sb.AppendLine($"Payé le {Fmt.Day(p.Date)} : {Fmt.Fcfa(p.Amount)}");
        sb.AppendLine($"Reste à payer : {Fmt.Fcfa(_invoice.Remaining)}");
        await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default
            .RequestAsync(new ShareTextRequest { Title = $"Facture {_invoice.Number}", Text = sb.ToString().TrimEnd() });
    }
}

[QueryProperty("InvoiceId", "invoiceId")]
public partial class PaymentViewModel : ObservableObject
{
    readonly ClinicService _db;
    long _remaining;
    public PaymentViewModel(ClinicService db) => _db = db;

    [ObservableProperty] string invoiceId = "";
    [ObservableProperty] string dateText = "";
    [ObservableProperty] string subtitle = "";
    [ObservableProperty] string amount = "";
    [ObservableProperty] string quickAmountLabel = "";

    public async Task LoadAsync()
    {
        if (await _db.GetInvoiceAsync(InvoiceId) is not { } inv) return;
        _remaining = inv.Remaining;
        DateText = Fmt.Day(inv.Date);
        Subtitle = $"{inv.Number} · reste {Fmt.Fcfa(_remaining)}";
        var quick = Math.Min(1000, _remaining);
        QuickAmountLabel = quick.ToString("N0", CultureInfo.GetCultureInfo("fr-FR"));
        Amount = _remaining.ToString(CultureInfo.InvariantCulture);
    }

    [RelayCommand] void All() => Amount = _remaining.ToString(CultureInfo.InvariantCulture);
    [RelayCommand] void Half() => Amount = (_remaining / 2).ToString(CultureInfo.InvariantCulture);
    [RelayCommand] void Quick() => Amount = Math.Min(1000, _remaining).ToString(CultureInfo.InvariantCulture);
    [RelayCommand] Task Cancel() => Nav.BackAsync();

    [RelayCommand]
    async Task Validate()
    {
        var digits = new string((Amount ?? "").Where(char.IsDigit).ToArray());
        if (!long.TryParse(digits, out var v) || v <= 0 || v > _remaining)
        { await Nav.AlertAsync("Montant invalide", $"Saisissez un montant entre 1 et {Fmt.Fcfa(_remaining)}."); return; }
        await _db.AddPaymentAsync(InvoiceId, v);
        await Nav.BackAsync();
    }
}

public partial class SearchViewModel : ObservableObject
{
    readonly ClinicService _db;
    CancellationTokenSource? _cts;
    public SearchViewModel(ClinicService db) => _db = db;

    public ObservableCollection<ConsultationRow> Consultations { get; } = new();
    public ObservableCollection<PatientRow> Patients { get; } = new();
    [ObservableProperty] string query = "";
    [ObservableProperty] string consultationsTitle = "Consultations (0)";
    [ObservableProperty] string patientsTitle = "Patients (0)";
    [ObservableProperty] bool hasQuery;

    partial void OnQueryChanged(string value) { HasQuery = value.Trim().Length > 0; _ = RunAsync(); }

    async Task RunAsync()
    {
        _cts?.Cancel(); _cts = new(); var token = _cts.Token;
        try { await Task.Delay(200, token); } catch (TaskCanceledException) { return; }
        var r = await _db.SearchAsync(Query);
        if (token.IsCancellationRequested) return;
        Consultations.Clear(); foreach (var c in r.Consultations) Consultations.Add(c);
        Patients.Clear(); foreach (var p in r.Patients) Patients.Add(p);
        ConsultationsTitle = $"Consultations ({r.Consultations.Count})";
        PatientsTitle = $"Patients ({r.Patients.Count})";
    }

    [RelayCommand] Task OpenConsultation(ConsultationRow r) => Nav.GoAsync(Nav.Routes.Consultation, ("id", r.Id));
    [RelayCommand] Task OpenPatient(PatientRow r) => Nav.GoAsync(Nav.Routes.Patient, ("id", r.Id));
}

public partial class SettingsViewModel : ObservableObject
{
    readonly ClinicService _db;
    readonly SyncService _sync;
    ClinicSettings _settings = new();
    bool _loading;
    public SettingsViewModel(ClinicService db, SyncService sync) { _db = db; _sync = sync; }

    [ObservableProperty] string clinicName = "";
    [ObservableProperty] string practitioner = "";
    [ObservableProperty] string phone = "";
    [ObservableProperty] string consultationFee = "";
    [ObservableProperty] bool darkTheme;
    [ObservableProperty] string serverUrl = "";
    [ObservableProperty] string apiKey = "";
    [ObservableProperty] string syncStatus = "";
    [ObservableProperty] bool isSyncing;

    partial void OnDarkThemeChanged(bool value)
    {
        if (_loading) return;
        AppPrefs.DarkTheme = value; AppPrefs.ApplyTheme();
    }

    public async Task LoadAsync()
    {
        _loading = true;
        _settings = await _db.GetSettingsAsync();
        ClinicName = _settings.ClinicName; Practitioner = _settings.Practitioner; Phone = _settings.Phone;
        ConsultationFee = _settings.ConsultationFee.ToString(CultureInfo.InvariantCulture);
        DarkTheme = AppPrefs.DarkTheme; ServerUrl = AppPrefs.ServerUrl; ApiKey = AppPrefs.ApiKey;
        SyncStatus = AppPrefs.LastSyncUtc == DateTime.MinValue ? "Jamais synchronisé"
            : $"Dernière synchronisation : {AppPrefs.LastSyncUtc.ToLocalTime():dd/MM/yyyy HH:mm}";
        _loading = false;
    }

    public async Task SaveAsync()
    {
        _settings.ClinicName = ClinicName.Trim(); _settings.Practitioner = Practitioner.Trim(); _settings.Phone = Phone.Trim();
        if (long.TryParse(new string(ConsultationFee.Where(char.IsDigit).ToArray()), out var fee)) _settings.ConsultationFee = fee;
        AppPrefs.ServerUrl = ServerUrl; AppPrefs.ApiKey = ApiKey;
        await _db.SaveSettingsAsync(_settings);
    }

    [RelayCommand] async Task Back() { await SaveAsync(); await Nav.BackAsync(); }

    [RelayCommand]
    async Task SyncNow()
    {
        IsSyncing = true;
        try
        {
            await SaveAsync();
            var r = await _sync.SyncAsync();
            SyncStatus = r.Message;
            await LoadAsync();
            if (r.Ok) SyncStatus = r.Message;
        }
        finally { IsSyncing = false; }
    }

    [RelayCommand]
    async Task Export()
    {
        var json = await _db.ExportJsonAsync();
        var path = Path.Combine(FileSystem.CacheDirectory, $"dossierclinic-sauvegarde-{DateTime.Now:yyyyMMdd-HHmm}.json");
        await File.WriteAllTextAsync(path, json);
        await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default
            .RequestAsync(new ShareFileRequest { Title = "Sauvegarde DossierClinic", File = new ShareFile(path) });
    }

    [RelayCommand]
    async Task Import()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choisir une sauvegarde (.json)" });
        if (file is null) return;
        if (!await Nav.ConfirmAsync("Importer", "Les données du fichier seront fusionnées avec celles de l'appareil.", "Importer")) return;
        try
        {
            await using var s = await file.OpenReadAsync();
            using var r = new StreamReader(s, Encoding.UTF8);
            await _db.ImportJsonAsync(await r.ReadToEndAsync());
            await Nav.AlertAsync("Importation", "Sauvegarde importée.");
        }
        catch (Exception) { await Nav.AlertAsync("Erreur", "Ce fichier n'est pas une sauvegarde DossierClinic valide."); }
    }

    [RelayCommand]
    async Task Wipe()
    {
        if (!await Nav.ConfirmAsync("Effacer toutes les données ?",
                "Tous les patients, consultations et factures de cet appareil seront supprimés. Cette action est irréversible.", "Effacer")) return;
        await _db.WipeAsync();
        await LoadAsync();
        await Nav.AlertAsync("Données effacées", "L'appareil est vide.");
    }
}
