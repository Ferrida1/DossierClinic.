using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;

namespace DossierClinic.Shared;

/// <summary>Base of every synchronised row: GUID key, last-write-wins timestamp, soft delete.</summary>
public abstract class SyncEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public void Touch() => UpdatedAt = DateTime.UtcNow;
}

public class Patient : SyncEntity
{
    public string Number { get; set; } = "";          // P-0001
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string Sex { get; set; } = "Femme";        // Femme / Homme
    public DateTime? BirthDate { get; set; }
    public string Phone { get; set; } = "";
    public string Quarter { get; set; } = "";
    public string BloodGroup { get; set; } = "";
    public string Allergies { get; set; } = "";

    [JsonIgnore] public List<Consultation> Consultations { get; set; } = new();

    [NotMapped, JsonIgnore] public string FullName => $"{FirstName} {LastName.ToUpperInvariant()}".Trim();
    [NotMapped, JsonIgnore] public string Initials =>
        (FirstName.Length > 0 ? FirstName[..1] : "") + (LastName.Length > 0 ? LastName[..1] : "");
    [NotMapped, JsonIgnore] public int? Age
    {
        get
        {
            if (BirthDate is not { } b) return null;
            var t = DateTime.Today; var a = t.Year - b.Year;
            if (b.Date > t.AddYears(-a)) a--;
            return Math.Max(a, 0);
        }
    }
    [NotMapped, JsonIgnore] public string SexAge => Age is { } a ? $"{Sex}, {a} ans" : Sex;
    [NotMapped, JsonIgnore] public bool HasAllergy => !string.IsNullOrWhiteSpace(Allergies);
}

public class Consultation : SyncEntity
{
    public string PatientId { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public string Reason { get; set; } = "";
    public double? Temperature { get; set; }
    public string BloodPressure { get; set; } = "";
    public double? Weight { get; set; }
    public string Diagnosis { get; set; } = "";
    public string Notes { get; set; } = "";

    [JsonIgnore] public Patient? Patient { get; set; }
    [JsonIgnore] public List<PrescriptionLine> Prescription { get; set; } = new();
    [JsonIgnore] public Invoice? Invoice { get; set; }
}

public class PrescriptionLine : SyncEntity
{
    public string ConsultationId { get; set; } = "";
    public int Position { get; set; }
    public string Drug { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Duration { get; set; } = "";
    [JsonIgnore] public Consultation? Consultation { get; set; }

    [NotMapped, JsonIgnore] public string Detail =>
        string.Join(" · ", new[] { Dosage, Duration }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public class Invoice : SyncEntity
{
    public string Number { get; set; } = "";          // F-2026-001
    public string PatientId { get; set; } = "";
    public string ConsultationId { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    [JsonIgnore] public Patient? Patient { get; set; }
    [JsonIgnore] public Consultation? Consultation { get; set; }
    [JsonIgnore] public List<InvoiceLine> Lines { get; set; } = new();
    [JsonIgnore] public List<Payment> Payments { get; set; } = new();

    [NotMapped, JsonIgnore] public long Total => Lines.Where(l => !l.IsDeleted).Sum(l => l.Amount);
    [NotMapped, JsonIgnore] public long Paid => Payments.Where(p => !p.IsDeleted).Sum(p => p.Amount);
    [NotMapped, JsonIgnore] public long Remaining => Math.Max(Total - Paid, 0);
    [NotMapped, JsonIgnore] public InvoiceStatus Status =>
        Remaining == 0 ? InvoiceStatus.Soldee : Paid > 0 ? InvoiceStatus.Partielle : InvoiceStatus.Impayee;
}

public enum InvoiceStatus { Impayee, Partielle, Soldee }

public class InvoiceLine : SyncEntity
{
    public string InvoiceId { get; set; } = "";
    public string Label { get; set; } = "";
    public long Amount { get; set; }
    [JsonIgnore] public Invoice? Invoice { get; set; }
}

public class Payment : SyncEntity
{
    public string InvoiceId { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public long Amount { get; set; }
    [JsonIgnore] public Invoice? Invoice { get; set; }
}

public class ClinicSettings : SyncEntity
{
    public ClinicSettings() { Id = "clinic"; }
    public string ClinicName { get; set; } = "Clinique Saint-Michel";
    public string Practitioner { get; set; } = "Dr A. Becken";
    public string Phone { get; set; } = "";
    public long ConsultationFee { get; set; } = 3000;
}

public static class Fmt
{
    static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    public static string Fcfa(long v) => v.ToString("N0", Fr).Replace(' ', ' ').Replace(' ', ' ') + " FCFA";
    public static string Day(DateTime d) => d.ToString("d MMM yyyy", Fr);
    public static string DayShort(DateTime d) => d.ToString("d MMM", Fr);
}
