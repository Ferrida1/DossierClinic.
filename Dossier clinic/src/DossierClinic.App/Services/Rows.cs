using DossierClinic.Shared;

namespace DossierClinic.Services;

/// <summary>Flat, display-ready rows used by list screens.</summary>
public record StatusStyle(string Text, Color Bg, Color Fg)
{
    public static StatusStyle For(InvoiceStatus s) => s switch
    {
        InvoiceStatus.Soldee => new("Soldée", Color.FromArgb("#E2F3EA"), Color.FromArgb("#1E7A4C")),
        InvoiceStatus.Partielle => new("Partielle", Color.FromArgb("#FBF0D9"), Color.FromArgb("#A66A00")),
        _ => new("Impayée", Color.FromArgb("#FBE9E5"), Color.FromArgb("#C2412D")),
    };
}

public class PatientRow
{
    public string Id { get; init; } = "";
    public string Number { get; init; } = "";
    public string Initials { get; init; } = "";
    public string Name { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public long Balance { get; init; }
    public bool HasBalance => Balance > 0;
    public string BalanceText => Fmt.Fcfa(Balance);
}

public class ConsultationRow
{
    public string Id { get; init; } = "";
    public string PatientName { get; init; } = "";
    public string Subtitle { get; init; } = "";
}

public class HistoryRow
{
    public string Id { get; init; } = "";
    public string DateText { get; init; } = "";
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public StatusStyle Status { get; init; } = StatusStyle.For(InvoiceStatus.Impayee);
}

public class InvoiceRow
{
    public string Id { get; init; } = "";
    public string PatientName { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string TotalText { get; init; } = "";
    public long Remaining { get; init; }
    public InvoiceStatus StatusKind { get; init; }
    public StatusStyle Status => StatusStyle.For(StatusKind);
}

public record LineRow(string Left, string Right);

public class InvoiceSummary
{
    public List<InvoiceRow> Rows { get; init; } = new();
    public long Collected { get; init; }
    public long Outstanding { get; init; }
}

public class SearchResult
{
    public List<ConsultationRow> Consultations { get; init; } = new();
    public List<PatientRow> Patients { get; init; } = new();
}
