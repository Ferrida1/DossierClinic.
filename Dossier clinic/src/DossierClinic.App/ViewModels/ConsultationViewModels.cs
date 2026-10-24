using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DossierClinic.Services;
using DossierClinic.Shared;

namespace DossierClinic.ViewModels;

public partial class ConsultationsViewModel : ObservableObject
{
    readonly ClinicService _db;
    public ConsultationsViewModel(ClinicService db) => _db = db;
    public ObservableCollection<ConsultationRow> Items { get; } = new();
    [ObservableProperty] string countText = "";

    public async Task LoadAsync()
    {
        var rows = await _db.GetConsultationsAsync();
        Items.Clear(); foreach (var r in rows) Items.Add(r);
        CountText = rows.Count switch { 0 => "Aucune consultation", 1 => "1 enregistrée", _ => $"{rows.Count} enregistrées" };
    }

    [RelayCommand] Task Open(ConsultationRow row) => Nav.GoAsync(Nav.Routes.Consultation, ("id", row.Id));
}

[QueryProperty("PatientId", "patientId")]
public partial class ConsultationFormViewModel : ObservableObject
{
    readonly ClinicService _db;
    readonly PrescriptionDraftStore _draft;
    Patient? _patient;
    bool _loaded;

    public ConsultationFormViewModel(ClinicService db, PrescriptionDraftStore draft) { _db = db; _draft = draft; }

    [ObservableProperty] string patientId = "";
    [ObservableProperty] string patientLine = "";
    [ObservableProperty] bool hasAllergy;
    [ObservableProperty] string allergyText = "";
    [ObservableProperty] DateTime date = DateTime.Today;
    [ObservableProperty] string reason = "";
    [ObservableProperty] string temperature = "";
    [ObservableProperty] string bloodPressure = "";
    [ObservableProperty] string weight = "";
    [ObservableProperty] string diagnosis = "";
    [ObservableProperty] string notes = "";
    [ObservableProperty] string prescriptionTitle = "Aucun médicament";
    [ObservableProperty] string prescriptionCount = "Ajouter";

    public async Task LoadAsync()
    {
        if (!_loaded)
        {
            _loaded = true;
            _draft.Lines.Clear();
            _patient = await _db.GetPatientAsync(PatientId);
            if (_patient is { } p)
            {
                PatientLine = p.Age is { } a ? $"{p.FullName} · {a} ans" : p.FullName;
                HasAllergy = p.HasAllergy; AllergyText = $"Allergie : {p.Allergies}";
            }
        }
        RefreshDraft();
    }

    public void RefreshDraft()
    {
        var n = _draft.Lines.Count;
        PrescriptionTitle = n == 0 ? "Aucun médicament" : _draft.Lines[0].Drug;
        PrescriptionCount = n == 0 ? "Ajouter" : n == 1 ? "1 ligne" : $"{n} lignes";
    }

    static double? ParseNumber(string s) =>
        double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

    [RelayCommand] Task EditPrescription() => Nav.GoAsync(Nav.Routes.Prescription);
    [RelayCommand] Task Cancel() => Nav.BackAsync();

    [RelayCommand]
    async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Reason)) { await Nav.AlertAsync("Champ requis", "Indiquez le motif de la consultation."); return; }
        var c = new Consultation
        {
            PatientId = PatientId, Date = Date.Date, Reason = Reason.Trim(), Temperature = ParseNumber(Temperature),
            BloodPressure = BloodPressure.Trim(), Weight = ParseNumber(Weight), Diagnosis = Diagnosis.Trim(), Notes = Notes.Trim()
        };
        var lines = _draft.Lines.Select(l => new PrescriptionLine { Drug = l.Drug, Dosage = l.Dosage, Duration = l.Duration }).ToList();
        await _db.CreateConsultationAsync(c, lines);
        _draft.Lines.Clear();
        await Nav.BackAsync();
    }
}

[QueryProperty("Id", "id")]
public partial class ConsultationDetailViewModel : ObservableObject
{
    readonly ClinicService _db;
    string _patientId = "", _invoiceId = "";
    public ConsultationDetailViewModel(ClinicService db) => _db = db;

    public ObservableCollection<PrescriptionLine> Prescription { get; } = new();
    public ObservableCollection<LineRow> InvoiceLines { get; } = new();

    [ObservableProperty] string id = "";
    [ObservableProperty] string dateText = "";
    [ObservableProperty] string patientName = "";
    [ObservableProperty] string patientSub = "";
    [ObservableProperty] bool hasAllergy;
    [ObservableProperty] string allergyText = "";
    [ObservableProperty] string reason = "";
    [ObservableProperty] string temperature = "";
    [ObservableProperty] string bloodPressure = "";
    [ObservableProperty] string weight = "";
    [ObservableProperty] string diagnosis = "";
    [ObservableProperty] string notes = "";
    [ObservableProperty] bool hasNotes;
    [ObservableProperty] bool hasInvoice;
    [ObservableProperty] string invoiceTitle = "";
    [ObservableProperty] StatusStyle invoiceStyle = StatusStyle.For(InvoiceStatus.Impayee);

    public async Task LoadAsync()
    {
        if (await _db.GetConsultationAsync(Id) is not { } c || c.Patient is not { } p) return;
        _patientId = p.Id;
        DateText = Fmt.Day(c.Date);
        PatientName = p.FullName; PatientSub = p.Age is { } a ? $"{p.Number} · {a} ans" : p.Number;
        HasAllergy = p.HasAllergy; AllergyText = $"Allergie : {p.Allergies}";
        Reason = c.Reason; Diagnosis = c.Diagnosis; BloodPressure = c.BloodPressure;
        Temperature = c.Temperature is { } t ? $"{t.ToString(CultureInfo.InvariantCulture)} °C" : "—";
        Weight = c.Weight is { } w ? $"{w.ToString(CultureInfo.InvariantCulture)} kg" : "—";
        Notes = c.Notes; HasNotes = !string.IsNullOrWhiteSpace(c.Notes);
        Prescription.Clear(); foreach (var l in c.Prescription) Prescription.Add(l);
        InvoiceLines.Clear();
        if (c.Invoice is { } inv)
        {
            HasInvoice = true; _invoiceId = inv.Id;
            InvoiceTitle = $"Facture {inv.Number}";
            InvoiceStyle = StatusStyle.For(inv.Status);
            foreach (var l in inv.Lines) InvoiceLines.Add(new LineRow(l.Label, Fmt.Fcfa(l.Amount)));
        }
        else HasInvoice = false;
    }

    [RelayCommand] Task Back() => Nav.BackAsync();
    [RelayCommand] Task OpenPatient() => Nav.GoAsync(Nav.Routes.Patient, ("id", _patientId));
    [RelayCommand] Task OpenInvoice() => Nav.GoAsync(Nav.Routes.Invoice, ("id", _invoiceId));
    [RelayCommand] Task EditPrescription() => Nav.GoAsync(Nav.Routes.Prescription, ("consultationId", Id));
    [RelayCommand] Task SharePrescription() => Nav.GoAsync(Nav.Routes.PrescriptionShare, ("consultationId", Id));
}

[QueryProperty("ConsultationId", "consultationId")]
public partial class PrescriptionViewModel : ObservableObject
{
    readonly ClinicService _db;
    readonly PrescriptionDraftStore _draft;
    bool _loaded;
    public PrescriptionViewModel(ClinicService db, PrescriptionDraftStore draft)
    { _db = db; _draft = draft; Lines = draft.Lines; }

    public ObservableCollection<PrescriptionLine> Lines { get; private set; }
    [ObservableProperty] string consultationId = "";
    [ObservableProperty] string subtitle = "Nouvelle consultation";
    [ObservableProperty] string newDrug = "";
    [ObservableProperty] string newDosage = "";
    [ObservableProperty] string newDuration = "";

    public async Task LoadAsync()
    {
        if (_loaded) return; _loaded = true;
        if (string.IsNullOrEmpty(ConsultationId)) return;
        // Editing a saved consultation: work on a private copy, written back on save.
        if (await _db.GetConsultationAsync(ConsultationId) is not { } c) return;
        Subtitle = $"Consultation du {Fmt.DayShort(c.Date)}";
        Lines = new ObservableCollection<PrescriptionLine>(c.Prescription);
        OnPropertyChanged(nameof(Lines));
    }

    void FlushPendingLine()
    {
        if (string.IsNullOrWhiteSpace(NewDrug)) return;
        Lines.Add(new PrescriptionLine { Drug = NewDrug.Trim(), Dosage = NewDosage.Trim(), Duration = NewDuration.Trim() });
        NewDrug = NewDosage = NewDuration = "";
    }

    [RelayCommand] void Add() => FlushPendingLine();
    [RelayCommand] void Remove(PrescriptionLine line) => Lines.Remove(line);
    [RelayCommand] Task Back() => Nav.BackAsync();

    [RelayCommand]
    async Task Save()
    {
        FlushPendingLine();
        if (!string.IsNullOrEmpty(ConsultationId)) await _db.ReplacePrescriptionAsync(ConsultationId, Lines);
        await Nav.BackAsync();
    }
}

[QueryProperty("ConsultationId", "consultationId")]
public partial class PrescriptionShareViewModel : ObservableObject
{
    readonly ClinicService _db;
    public PrescriptionShareViewModel(ClinicService db) => _db = db;
    [ObservableProperty] string consultationId = "";
    [ObservableProperty] string dateText = "";
    [ObservableProperty] string text = "";

    public async Task LoadAsync()
    {
        if (await _db.GetConsultationAsync(ConsultationId) is not { } c || c.Patient is not { } p) return;
        DateText = Fmt.Day(c.Date);
        Text = ClinicService.BuildPrescriptionText(c, p, c.Prescription, await _db.GetSettingsAsync());
    }

    [RelayCommand] Task Close() => Nav.BackAsync();
    [RelayCommand] Task Share() => Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default
        .RequestAsync(new ShareTextRequest { Title = "Ordonnance", Text = Text });
}
