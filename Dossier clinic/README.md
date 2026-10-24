# DossierClinic — .NET MAUI + ASP.NET Core

Mobile clinic app generated from the 14 Figma screens (patients, consultations, prescriptions, invoices, payments, search, settings).

## Architecture
| Project | What it is |
|---|---|
| `src/DossierClinic.Shared` | EF Core entities, `ClinicDbContext` (SQLite), last-write-wins `SyncEngine`, sync DTOs |
| `src/DossierClinic.App` | .NET MAUI app (Android + iOS), MVVM (CommunityToolkit.Mvvm), **offline-first** local SQLite |
| `src/DossierClinic.Api` | ASP.NET Core 8 minimal API + SQLite: `/api/sync`, CRUD/read endpoints, `/api/stats`, Swagger |

The app works fully offline. Under **Paramètres → Serveur** enter the API address and key, then *Synchroniser maintenant* to push/pull changes.

## Run
```bash
# API  (default key: change-me-in-production — set ApiKey in appsettings.json or env var ApiKey)
dotnet run --project src/DossierClinic.Api --urls http://0.0.0.0:5080
#   Swagger: http://localhost:5080/swagger   (send header X-Api-Key on /api calls)

# App (needs: dotnet workload install maui)
dotnet build src/DossierClinic.App -f net8.0-android -t:Run
```
Android emulator → server address `http://10.0.2.2:5080`. Physical phone → your PC's LAN IP. Use HTTPS in production (and remove `usesCleartextTraffic` / ATS exception).
Docker: `docker build -f src/DossierClinic.Api/Dockerfile -t dossierclinic-api .`

## Screens → files (`src/DossierClinic.App/Views`)
01 Splash `SplashPage` · 02 `PatientsPage` · 03 `PatientFormPage` · 04 `PatientDetailPage` · 05 `ConsultationFormPage` · 06 `ConsultationDetailPage` · 07 `PrescriptionPage` · 08 `PrescriptionSharePage` · 09 `InvoicesPage` · 10 `InvoiceDetailPage` · 11 `PaymentPage` · 12 `SearchPage` · 13 `ConsultationsPage` · 14 `SettingsPage`

## Behaviour notes
- Saving a consultation auto-creates its invoice (line "Consultation", price = *Tarif de consultation* in settings, default 3 000 FCFA). Status Impayée / Partielle / Soldée is derived from payments.
- Amounts are integers (FCFA). IDs are GUIDs; patient numbers (P-0001) and invoice numbers (F-2026-001) are generated per device.
- Sync = last-write-wins per row using UTC timestamps, soft deletes, API-key header. It assumes reasonably accurate device clocks.
- Backup = JSON export/import via the system share sheet / file picker.
- Fonts (Figtree, Bricolage Grotesque) are not bundled; drop the .ttf files in `Resources/Fonts` and register them in `MauiProgram`.

## Not yet built/tested
Generated without a .NET SDK available, so it has **not been compiled**. Expect to fix at most a few small build errors on first build.
