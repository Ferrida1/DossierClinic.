using Microsoft.EntityFrameworkCore;

namespace DossierClinic.Shared;

public class SyncChanges
{
    public List<Patient> Patients { get; set; } = new();
    public List<Consultation> Consultations { get; set; } = new();
    public List<PrescriptionLine> PrescriptionLines { get; set; } = new();
    public List<Invoice> Invoices { get; set; } = new();
    public List<InvoiceLine> InvoiceLines { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public List<ClinicSettings> Settings { get; set; } = new();
    public int Count => Patients.Count + Consultations.Count + PrescriptionLines.Count + Invoices.Count
                      + InvoiceLines.Count + Payments.Count + Settings.Count;
}

public class SyncRequest
{
    public DateTime LastSync { get; set; }
    public SyncChanges Changes { get; set; } = new();
}

public class SyncResponse
{
    public DateTime ServerTime { get; set; }
    public SyncChanges Changes { get; set; } = new();
}

/// <summary>Last-write-wins synchronisation shared by the device and the API.</summary>
public static class SyncEngine
{
    public static async Task<SyncChanges> CollectAsync(ClinicDbContext db, DateTime since)
    {
        // Parents before children so foreign keys are satisfied on the receiving side.
        return new SyncChanges
        {
            Patients = await db.Patients.IgnoreQueryFilters().AsNoTracking().Where(x => x.UpdatedAt > since).ToListAsync(),
            Consultations = await db.Consultations.IgnoreQueryFilters().AsNoTracking().Where(x => x.UpdatedAt > since).ToListAsync(),
            PrescriptionLines = await db.PrescriptionLines.IgnoreQueryFilters().AsNoTracking().Where(x => x.UpdatedAt > since).ToListAsync(),
            Invoices = await db.Invoices.IgnoreQueryFilters().AsNoTracking().Where(x => x.UpdatedAt > since).ToListAsync(),
            InvoiceLines = await db.InvoiceLines.IgnoreQueryFilters().AsNoTracking().Where(x => x.UpdatedAt > since).ToListAsync(),
            Payments = await db.Payments.IgnoreQueryFilters().AsNoTracking().Where(x => x.UpdatedAt > since).ToListAsync(),
            Settings = await db.Settings.AsNoTracking().Where(x => x.UpdatedAt > since).ToListAsync(),
        };
    }

    /// <param name="restamp">When true (server), accepted rows get the server clock so every device sees them on its next pull.</param>
    public static async Task ApplyAsync(ClinicDbContext db, SyncChanges c, bool restamp)
    {
        await Upsert(db, db.Patients, c.Patients, restamp);
        await Upsert(db, db.Consultations, c.Consultations, restamp);
        await Upsert(db, db.PrescriptionLines, c.PrescriptionLines, restamp);
        await Upsert(db, db.Invoices, c.Invoices, restamp);
        await Upsert(db, db.InvoiceLines, c.InvoiceLines, restamp);
        await Upsert(db, db.Payments, c.Payments, restamp);
        await Upsert(db, db.Settings, c.Settings, restamp);
        await db.SaveChangesAsync();
    }

    static async Task Upsert<T>(ClinicDbContext db, DbSet<T> set, List<T> incoming, bool restamp) where T : SyncEntity
    {
        foreach (var item in incoming)
        {
            var existing = await set.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == item.Id);
            if (existing is null)
            {
                if (restamp) item.UpdatedAt = DateTime.UtcNow;
                set.Add(item);
            }
            else if (item.UpdatedAt >= existing.UpdatedAt)
            {
                var stamp = restamp ? DateTime.UtcNow : item.UpdatedAt;
                db.Entry(existing).CurrentValues.SetValues(item);
                existing.UpdatedAt = stamp;
            }
        }
    }
}
