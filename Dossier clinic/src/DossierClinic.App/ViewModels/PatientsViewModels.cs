using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DossierClinic.Services;
using DossierClinic.Shared;

namespace DossierClinic.ViewModels;

public partial class PatientsViewModel : ObservableObject
{
    readonly ClinicService _db;
    public PatientsViewModel(ClinicService db) => _db = db;

    public ObservableCollection<PatientRow> Items { get; } = new();
    [ObservableProperty] string query = "";
    [ObservableProperty] string countText = "";

    partial void OnQueryChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        var rows = await _db.GetPatientsAsync(Query);
        Items.Clear();
        foreach (var r in rows) Items.Add(r);
        CountText = rows.Count switch { 0 => "Aucun dossier", 1 => "1 dossier", _ => $"{rows.Count} dossiers" };
    }

    [RelayCommand] Task NewPatient() => Nav.GoAsync(Nav.Routes.PatientForm);
    [RelayCommand] Task Open(PatientRow row) => Nav.GoAsync(Nav.Routes.Patient, ("id", row.Id));
    [RelayCommand] Task OpenSettings() => Nav.GoAsync(Nav.Routes.Settings);
}

[QueryProperty("Id", "id")]
public partial class PatientFormViewModel : ObservableObject
{
    readonly ClinicService _db;
    Patient _patient = new();
    bool _loaded;
    public PatientFormViewModel(ClinicService db) => _db = db;

    public string[] Sexes { get; } = { "Femme", "Homme" };
    public string[] BloodGroups { get; } = { "", "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };

    [ObservableProperty] string id = "";
    [ObservableProperty] string title = "Nouveau patient";
    [ObservableProperty] string lastName = "";
    [ObservableProperty] string firstName = "";
    [ObservableProperty] string sex = "Femme";
    [ObservableProperty] DateTime birthDate = new(1990, 1, 1);
    [ObservableProperty] string phone = "";
    [ObservableProperty] string quarter = "";
    [ObservableProperty] string bloodGroup = "";
    [ObservableProperty] string allergies = "";

    public async Task LoadAsync()
    {
        if (_loaded) return; _loaded = true;
        if (string.IsNullOrEmpty(Id)) return;
        if (await _db.GetPatientAsync(Id) is not { } p) return;
        _patient = p; Title = "Modifier le patient";
        LastName = p.LastName; FirstName = p.FirstName; Sex = p.Sex; BirthDate = p.BirthDate ?? BirthDate;
        Phone = p.Phone; Quarter = p.Quarter; BloodGroup = p.BloodGroup; Allergies = p.Allergies;
    }

    [RelayCommand]
    async Task Save()
    {
        if (string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(FirstName))
        { await Nav.AlertAsync("Champs requis", "Le nom et le prénom sont obligatoires."); return; }
        _patient.LastName = LastName.Trim(); _patient.FirstName = FirstName.Trim(); _patient.Sex = Sex;
        _patient.BirthDate = BirthDate.Date; _patient.Phone = Phone.Trim(); _patient.Quarter = Quarter.Trim();
        _patient.BloodGroup = BloodGroup; _patient.Allergies = Allergies.Trim();
        await _db.SavePatientAsync(_patient);
        await Nav.BackAsync();
    }

    [RelayCommand] Task Cancel() => Nav.BackAsync();
}

[QueryProperty("Id", "id")]
public partial class PatientDetailViewModel : ObservableObject
{
    readonly ClinicService _db;
    public PatientDetailViewModel(ClinicService db) => _db = db;

    public ObservableCollection<HistoryRow> History { get; } = new();
    [ObservableProperty] string id = "";
    [ObservableProperty] string name = "";
    [ObservableProperty] string number = "";
    [ObservableProperty] string sexAge = "";
    [ObservableProperty] string birthText = "";
    [ObservableProperty] string phone = "";
    [ObservableProperty] string quarter = "";
    [ObservableProperty] string bloodGroup = "";
    [ObservableProperty] bool hasAllergy;
    [ObservableProperty] string allergyText = "";
    [ObservableProperty] bool hasBalance;
    [ObservableProperty] string balanceText = "";
    [ObservableProperty] string historyTitle = "Historique (0)";

    public async Task LoadAsync()
    {
        if (await _db.GetPatientAsync(Id) is not { } p) return;
        Name = p.FullName; Number = p.Number; SexAge = p.SexAge;
        BirthText = p.BirthDate is { } b ? Fmt.Day(b) : "—";
        Phone = p.Phone; Quarter = p.Quarter; BloodGroup = p.BloodGroup;
        HasAllergy = p.HasAllergy; AllergyText = $"Allergie : {p.Allergies}";
        var bal = await _db.GetPatientBalanceAsync(Id);
        HasBalance = bal > 0; BalanceText = $"Reste à payer : {Fmt.Fcfa(bal)}";
        var rows = await _db.GetHistoryAsync(Id);
        History.Clear(); foreach (var r in rows) History.Add(r);
        HistoryTitle = $"Historique ({rows.Count})";
    }

    [RelayCommand] Task Back() => Nav.BackAsync();
    [RelayCommand] Task NewConsultation() => Nav.GoAsync(Nav.Routes.ConsultationForm, ("patientId", Id));
    [RelayCommand] Task Edit() => Nav.GoAsync(Nav.Routes.PatientForm, ("id", Id));
    [RelayCommand] Task OpenConsultation(HistoryRow row) => Nav.GoAsync(Nav.Routes.Consultation, ("id", row.Id));
}
