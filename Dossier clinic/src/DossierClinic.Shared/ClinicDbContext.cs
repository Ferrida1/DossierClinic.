using Microsoft.EntityFrameworkCore;

namespace DossierClinic.Shared;

public class ClinicDbContext : DbContext
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<PrescriptionLine> PrescriptionLines => Set<PrescriptionLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ClinicSettings> Settings => Set<ClinicSettings>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Consultation>().HasOne(c => c.Patient).WithMany(p => p.Consultations)
            .HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PrescriptionLine>().HasOne(l => l.Consultation).WithMany(c => c.Prescription)
            .HasForeignKey(l => l.ConsultationId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Invoice>().HasOne(i => i.Consultation).WithOne(c => c.Invoice)
            .HasForeignKey<Invoice>(i => i.ConsultationId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Invoice>().HasOne(i => i.Patient).WithMany()
            .HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InvoiceLine>().HasOne(l => l.Invoice).WithMany(i => i.Lines)
            .HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Payment>().HasOne(p => p.Invoice).WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId).OnDelete(DeleteBehavior.Cascade);

        // Soft delete: deleted rows are hidden everywhere except in the sync engine.
        b.Entity<Patient>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Consultation>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<PrescriptionLine>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Invoice>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<InvoiceLine>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Payment>().HasQueryFilter(x => !x.IsDeleted);

        b.Entity<Patient>().HasIndex(p => p.Number);
        b.Entity<Patient>().HasIndex(p => p.UpdatedAt);
        b.Entity<Consultation>().HasIndex(c => c.UpdatedAt);
        b.Entity<Invoice>().HasIndex(i => i.UpdatedAt);
    }
}
