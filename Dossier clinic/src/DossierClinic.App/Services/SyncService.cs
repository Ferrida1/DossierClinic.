using System.Net.Http.Json;
using DossierClinic.Shared;
using Microsoft.EntityFrameworkCore;

namespace DossierClinic.Services;

public record SyncOutcome(bool Ok, string Message);

/// <summary>Optional push/pull with the DossierClinic API. The app works fully without it.</summary>
public class SyncService
{
    readonly IDbContextFactory<ClinicDbContext> _factory;
    public SyncService(IDbContextFactory<ClinicDbContext> factory) => _factory = factory;

    public async Task<SyncOutcome> SyncAsync()
    {
        var url = AppPrefs.ServerUrl.TrimEnd('/');
        if (url.Length == 0) return new(false, "Renseignez l'adresse du serveur.");
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(url + "/"), Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.Add("X-Api-Key", AppPrefs.ApiKey);

            await using var db = await _factory.CreateDbContextAsync();
            var since = AppPrefs.LastSyncUtc;
            var outgoing = await SyncEngine.CollectAsync(db, since);

            var resp = await http.PostAsJsonAsync("api/sync", new SyncRequest { LastSync = since, Changes = outgoing });
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized) return new(false, "Clé API refusée.");
            resp.EnsureSuccessStatusCode();
            var data = await resp.Content.ReadFromJsonAsync<SyncResponse>() ?? throw new InvalidDataException();

            await SyncEngine.ApplyAsync(db, data.Changes, restamp: false);

            // Rows just received carry server stamps; move the watermark past them so they are not echoed back.
            var newest = new[]
            {
                data.Changes.Patients.Select(x => x.UpdatedAt), data.Changes.Consultations.Select(x => x.UpdatedAt),
                data.Changes.PrescriptionLines.Select(x => x.UpdatedAt), data.Changes.Invoices.Select(x => x.UpdatedAt),
                data.Changes.InvoiceLines.Select(x => x.UpdatedAt), data.Changes.Payments.Select(x => x.UpdatedAt),
                data.Changes.Settings.Select(x => x.UpdatedAt)
            }.SelectMany(x => x).DefaultIfEmpty(DateTime.MinValue).Max();
            AppPrefs.LastSyncUtc = newest > data.ServerTime ? newest : data.ServerTime;

            return new(true, $"Synchronisé : {outgoing.Count} envoyé(s), {data.Changes.Count} reçu(s).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            return new(false, "Serveur injoignable. Les données restent sur l'appareil.");
        }
    }
}
