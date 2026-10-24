using System.Text;
using System.Text.Json;
using DossierClinic.Shared;
using Microsoft.EntityFrameworkCore;

namespace DossierClinic.Services;

/// <summary>All local (offline-first) data access. The SQLite file lives on the device.</summary>
public class ClinicService
{
    readonly IDbContextFactory<ClinicDbContext> _factory;
    public ClinicService(IDbContextFactory<ClinicDbContext> factory) => _factory = factory;

    public static string DbPath => Path.Combine(FileSystem.AppDataDirectory, "dossierclinic.db");

    public async Task InitAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        if (!await db.Settings.AnyAsync()) { db.Settings.Add(new ClinicSettings()); await db.SaveChangesAsync(); }
    }

    // ---------- Settings ----------
    public async Task<ClinicSettings> GetSettingsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Settings.AsNoTracking().FirstOrDefaultAsync() ?? new ClinicSettings();
    }

    public async Task SaveSettingsAsync(ClinicSettings s)
    {
        await using var db = await _factory.CreateDbContextAsync();
        s.Id = "clinic"; s.Touch();
        if (await db.Settings.AnyAsync(x => x.Id == "clinic")) db.Settings.Update(s); else db.Settings.Add(s);
        await db.SaveChangesAsync();
    }

    // ---------- Patients ----------
    public async Task<List<PatientRow>> GetPatientsAsync(string? q = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Patients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.LastName, like) || EF.Functions.Like(p.FirstName, like) ||
                                     EF.Functions.Like(p.Phone, like) || EF.Functions.Like(p.Number, like));
        }
        var patients = await query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync();
        var balances = await BalancesAsync(db);
        return patients.Select(p => ToRow(p, balances.GetValueOrDefault(p.Id))).ToList();
    }

    static PatientRow ToRow(Patient p, long balance) => new()
    {
        Id = p.Id, Number = p.Number, Initials = p.Initials.ToUpperInvariant(), Name = p.FullName,
        Subtitle = string.IsNullOrWhiteSpace(p.Phone) ? p.SexAge : $"{p.SexAge} · {p.Phone}",
        Balance = balance
    };

    static async Task<Dictionary<string, long>> BalancesAsync(ClinicDbContext db)
    {
        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Lines).Include(i => i.Payments).ToListAsync();
        return invoices.GroupBy(i => i.PatientId).ToDictionary(g => g.Key, g => g.Sum(i => i.Remaining));
    }

    public async Task<Patient?> GetPatientAsync(string id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<long> GetPatientBalanceAsync(string id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return (await BalancesAsync(db)).GetValueOrDefault(id);
    }

    public async Task SavePatientAsync(Patient p)
    {
        await using var db = await _factory.CreateDbContextAsync();
        p.Touch();
        if (await db.Patients.AnyAsync(x => x.Id == p.Id)) db.Patients.Update(p);
        else
        {
            var count = await db.Patients.IgnoreQueryFilters().CountAsync();
            p.Number = $"P-{count + 1:0000}";
            db.Patients.Add(p);
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<HistoryRow>> GetHistoryAsync(string patientId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var list = await db.Consultations.AsNoTracking().Where(c => c.PatientId == patientId)
            .Include(c => c.Prescription).Include(c => c.Invoice).ThenInclude(i => i!.Lines)
            .Include(c => c.Invoice).ThenInclude(i => i!.Payments)
            .OrderByDescending(c => c.Date).ToListAsync();
        return list.Select(c => new HistoryRow
        {
            Id = c.Id, DateText = Fmt.Day(c.Date),
            Title = string.IsNullOrWhiteSpace(c.Diagnosis) ? c.Reason : c.Diagnosis,
            Subtitle = c.Prescription.Count == 0 ? c.Reason : $"{c.Reason} · {c.Prescription.Count} médicament{(c.Prescription.Count > 1 ? "s" : "")}",
            Status = StatusStyle.For(c.Invoice?.Status ?? InvoiceStatus.Soldee)
        }).ToList();
    }

    // ---------- Consultations ----------
    public async Task<List<ConsultationRow>> GetConsultationsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var list = await db.Consultations.AsNoTracking().Include(c => c.Patient)
            .OrderByDescending(c => c.Date).ToListAsync();
        return list.Select(ToRow).ToList();
    }

    static ConsultationRow ToRow(Consultation c) => new()
    {
        Id = c.Id, PatientName = c.Patient?.FullName ?? "",
        Subtitle = $"{Fmt.Day(c.Date)} · {(string.IsNullOrWhiteSpace(c.Diagnosis) ? c.Reason : c.Diagnosis)}"
    };

    public async Task<Consultation?> GetConsultationAsync(string id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var c = await db.Consultations.AsNoTracking().Include(x => x.Patient).Include(x => x.Prescription)
            .Include(x => x.Invoice).ThenInclude(i => i!.Lines)
            .Include(x => x.Invoice).ThenInclude(i => i!.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);
        c?.Prescription.Sort((a, b) => a.Position.CompareTo(b.Position));
        return c;
    }

    /// <summary>Saves a new consultation with its prescription and creates the matching invoice.</summary>
    public async Task<Consultation> CreateConsultationAsync(Consultation c, IEnumerable<PrescriptionLine> lines)
    {
        await using var db = await _factory.CreateDbContextAsync();
        c.Touch();
        db.Consultations.Add(c);
        int pos = 0;
        foreach (var l in lines)
        {
            l.ConsultationId = c.Id; l.Position = pos++; l.Touch();
            db.PrescriptionLines.Add(l);
        }
        var fee = (await db.Settings.AsNoTracking().FirstOrDefaultAsync())?.ConsultationFee ?? 3000;
        var year = c.Date.Year;
        var seq = await db.Invoices.IgnoreQueryFilters().CountAsync(i => i.Number.StartsWith($"F-{year}-")) + 1;
        var inv = new Invoice { Number = $"F-{year}-{seq:000}", PatientId = c.PatientId, ConsultationId = c.Id, Date = c.Date };
        db.Invoices.Add(inv);
        db.InvoiceLines.Add(new InvoiceLine { InvoiceId = inv.Id, Label = "Consultation", Amount = fee });
        await db.SaveChangesAsync();
        return c;
    }

    public async Task ReplacePrescriptionAsync(string consultationId, IEnumerable<PrescriptionLine> lines)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var old = await db.PrescriptionLines.Where(l => l.ConsultationId == consultationId).ToListAsync();
        foreach (var o in old) { o.IsDeleted = true; o.Touch(); }
        int pos = 0;
        foreach (var l in lines)
        {
            db.PrescriptionLines.Add(new PrescriptionLine
            {
                ConsultationId = consultationId, Position = pos++, Drug = l.Drug, Dosage = l.Dosage, Duration = l.Duration
            });
        }
        var cons = await db.Consultations.FirstAsync(c => c.Id == consultationId);
        cons.Touch();
        await db.SaveChangesAsync();
    }

    // ---------- Invoices & payments ----------
    public async Task<InvoiceSummary> GetInvoicesAsync(string filter = "Toutes")
    {
        await using var db = await _factory.CreateDbContextAsync();
        var all = await db.Invoices.AsNoTracking().Include(i => i.Patient).Include(i => i.Lines).Include(i => i.Payments)
            .OrderByDescending(i => i.Date).ThenByDescending(i => i.Number).ToListAsync();
        var filtered = filter switch
        {
            "À encaisser" => all.Where(i => i.Remaining > 0),
            "Soldées" => all.Where(i => i.Remaining == 0),
            _ => all
        };
        return new InvoiceSummary
        {
            Collected = all.Sum(i => i.Paid),
            Outstanding = all.Sum(i => i.Remaining),
            Rows = filtered.Select(i => new InvoiceRow
            {
                Id = i.Id, PatientName = i.Patient?.FullName ?? "",
                Subtitle = $"{i.Number} · {Fmt.Day(i.Date)}",
                TotalText = Fmt.Fcfa(i.Total), Remaining = i.Remaining, StatusKind = i.Status
            }).ToList()
        };
    }

    public async Task<Invoice?> GetInvoiceAsync(string id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var inv = await db.Invoices.AsNoTracking().Include(i => i.Patient).Include(i => i.Consultation)
            .Include(i => i.Lines).Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id);
        inv?.Payments.Sort((a, b) => a.Date.CompareTo(b.Date));
        return inv;
    }

    public async Task AddPaymentAsync(string invoiceId, long amount)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var inv = await db.Invoices.Include(i => i.Lines).Include(i => i.Payments).FirstAsync(i => i.Id == invoiceId);
        if (amount <= 0 || amount > inv.Remaining) throw new InvalidOperationException("Montant invalide.");
        db.Payments.Add(new Payment { InvoiceId = invoiceId, Amount = amount, Date = DateTime.Today });
        inv.Touch();
        await db.SaveChangesAsync();
    }

    // ---------- Search ----------
    public async Task<SearchResult> SearchAsync(string q)
    {
        q = q.Trim();
        if (q.Length == 0) return new();
        var like = $"%{q}%";
        await using var db = await _factory.CreateDbContextAsync();
        var cons = await db.Consultations.AsNoTracking().Include(c => c.Patient)
            .Where(c => EF.Functions.Like(c.Reason, like) || EF.Functions.Like(c.Diagnosis, like) ||
                        EF.Functions.Like(c.Notes, like) ||
                        (c.Patient != null && (EF.Functions.Like(c.Patient.LastName, like) || EF.Functions.Like(c.Patient.FirstName, like))) ||
                        db.PrescriptionLines.Any(l => l.ConsultationId == c.Id && EF.Functions.Like(l.Drug, like)))
            .OrderByDescending(c => c.Date).ToListAsync();
        return new SearchResult { Consultations = cons.Select(ToRow).ToList(), Patients = await GetPatientsAsync(q) };
    }

    // ---------- Backup / restore / wipe ----------
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<string> ExportJsonAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return JsonSerializer.Serialize(await SyncEngine.CollectAsync(db, DateTime.MinValue), Json);
    }

    public async Task ImportJsonAsync(string json)
    {
        var data = JsonSerializer.Deserialize<SyncChanges>(json, Json) ?? throw new InvalidDataException();
        await using var db = await _factory.CreateDbContextAsync();
        await SyncEngine.ApplyAsync(db, data, restamp: false);
    }

    public async Task WipeAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        db.Settings.Add(new ClinicSettings());
        await db.SaveChangesAsync();
        AppPrefs.LastSyncUtc = DateTime.MinValue;
    }

    // ---------- Prescription text for sharing ----------
    public static string BuildPrescriptionText(Consultation c, Patient p, IReadOnlyList<PrescriptionLine> lines, ClinicSettings s)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ORDONNANCE du {Fmt.Day(c.Date)}");
        sb.AppendLine($"Patient : {p.FullName}{(p.Age is { } a ? $" ({a} ans)" : "")}");
        if (p.HasAllergy) sb.AppendLine($"Allergie : {p.Allergies}");
        sb.AppendLine();
        for (int i = 0; i < lines.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {lines[i].Drug}");
            var d = string.Join(" pendant ", new[] { lines[i].Dosage, lines[i].Duration }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (d.Length > 0) sb.AppendLine($"   {d}");
        }
        sb.AppendLine();
        sb.AppendLine($"{s.Practitioner} — {s.ClinicName}");
        return sb.ToString().TrimEnd();
    }
}
