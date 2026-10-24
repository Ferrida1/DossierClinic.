using DossierClinic.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
Directory.CreateDirectory("App_Data");

builder.Services.AddDbContext<ClinicDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Clinic")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

// Simple shared-secret protection for everything under /api (header: X-Api-Key).
var apiKey = app.Configuration["ApiKey"];
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api") &&
        (!ctx.Request.Headers.TryGetValue("X-Api-Key", out var k) || k != apiKey))
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await ctx.Response.WriteAsync("Clé API invalide.");
        return;
    }
    await next();
});

app.MapGet("/", () => "DossierClinic API");
app.MapGet("/api/ping", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));

// ---- Device synchronisation (last-write-wins) -------------------------------------------
app.MapPost("/api/sync", async (SyncRequest req, ClinicDbContext db) =>
{
    var serverTime = DateTime.UtcNow;
    await SyncEngine.ApplyAsync(db, req.Changes, restamp: true);
    var changes = await SyncEngine.CollectAsync(db, req.LastSync);
    return Results.Ok(new SyncResponse { ServerTime = serverTime, Changes = changes });
});

// ---- Read/CRUD endpoints for dashboards, web back-office, integrations -------------------
var api = app.MapGroup("/api");

api.MapGet("/patients", async (string? q, ClinicDbContext db) =>
{
    var query = db.Patients.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
        query = query.Where(p => p.LastName.Contains(q) || p.FirstName.Contains(q) ||
                                 p.Phone.Contains(q) || p.Number.Contains(q));
    return await query.OrderBy(p => p.LastName).ToListAsync();
});
api.MapGet("/patients/{id}", async (string id, ClinicDbContext db) =>
    await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id) is { } p ? Results.Ok(p) : Results.NotFound());
api.MapPut("/patients/{id}", async (string id, Patient body, ClinicDbContext db) =>
{
    var p = await db.Patients.FirstOrDefaultAsync(x => x.Id == id);
    if (p is null) { body.Id = id; body.Touch(); db.Patients.Add(body); }
    else { db.Entry(p).CurrentValues.SetValues(body); p.Id = id; p.Touch(); }
    await db.SaveChangesAsync();
    return Results.NoContent();
});
api.MapDelete("/patients/{id}", async (string id, ClinicDbContext db) =>
{
    var p = await db.Patients.FirstOrDefaultAsync(x => x.Id == id);
    if (p is null) return Results.NotFound();
    p.IsDeleted = true; p.Touch();
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapGet("/patients/{id}/consultations", async (string id, ClinicDbContext db) =>
    await db.Consultations.AsNoTracking().Where(c => c.PatientId == id).OrderByDescending(c => c.Date).ToListAsync());
api.MapGet("/consultations", async (ClinicDbContext db) =>
    await db.Consultations.AsNoTracking().OrderByDescending(c => c.Date).ToListAsync());
api.MapGet("/consultations/{id}", async (string id, ClinicDbContext db) =>
{
    var c = await db.Consultations.AsNoTracking().Include(x => x.Prescription).Include(x => x.Patient)
        .Include(x => x.Invoice).ThenInclude(i => i!.Lines)
        .FirstOrDefaultAsync(x => x.Id == id);
    if (c is null) return Results.NotFound();
    return Results.Ok(new
    {
        Consultation = c,
        Patient = c.Patient,
        Prescription = c.Prescription.OrderBy(l => l.Position),
        Invoice = c.Invoice
    });
});

api.MapGet("/invoices", async (ClinicDbContext db) =>
{
    var list = await db.Invoices.AsNoTracking().Include(i => i.Lines).Include(i => i.Payments)
        .OrderByDescending(i => i.Date).ToListAsync();
    return list.Select(i => new
    {
        i.Id, i.Number, i.PatientId, i.ConsultationId, i.Date, i.Total, i.Paid, i.Remaining,
        Status = i.Status.ToString()
    });
});
api.MapPost("/invoices/{id}/payments", async (string id, PaymentBody body, ClinicDbContext db) =>
{
    var inv = await db.Invoices.Include(i => i.Lines).Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id);
    if (inv is null) return Results.NotFound();
    if (body.Amount <= 0 || body.Amount > inv.Remaining) return Results.BadRequest("Montant invalide.");
    db.Payments.Add(new Payment { InvoiceId = id, Amount = body.Amount });
    inv.Touch();
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapGet("/stats", async (ClinicDbContext db) =>
{
    var invoices = await db.Invoices.AsNoTracking().Include(i => i.Lines).Include(i => i.Payments).ToListAsync();
    return new
    {
        Patients = await db.Patients.CountAsync(),
        Consultations = await db.Consultations.CountAsync(),
        Invoiced = invoices.Sum(i => i.Total),
        Collected = invoices.Sum(i => i.Paid),
        Outstanding = invoices.Sum(i => i.Remaining)
    };
});

app.Run();

record PaymentBody(long Amount);
