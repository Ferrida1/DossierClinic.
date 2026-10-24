using DossierClinic.Services;
using DossierClinic.ViewModels;
using DossierClinic.Views;
using DossierClinic.Shared;
using Microsoft.EntityFrameworkCore;

namespace DossierClinic;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        // Drop Figtree-Regular/SemiBold.ttf and BricolageGrotesque-Bold.ttf into Resources/Fonts,
        // then register them here with builder.ConfigureFonts(f => f.AddFont(...)).

        builder.Services.AddDbContextFactory<ClinicDbContext>(o => o.UseSqlite($"Data Source={ClinicService.DbPath}"));
        builder.Services.AddSingleton<ClinicService>();
        builder.Services.AddSingleton<SyncService>();
        builder.Services.AddSingleton<PrescriptionDraftStore>();

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<SplashPage>();

        builder.Services.AddTransient<PatientsPage>().AddTransient<PatientsViewModel>();
        builder.Services.AddTransient<PatientFormPage>().AddTransient<PatientFormViewModel>();
        builder.Services.AddTransient<PatientDetailPage>().AddTransient<PatientDetailViewModel>();
        builder.Services.AddTransient<ConsultationsPage>().AddTransient<ConsultationsViewModel>();
        builder.Services.AddTransient<ConsultationFormPage>().AddTransient<ConsultationFormViewModel>();
        builder.Services.AddTransient<ConsultationDetailPage>().AddTransient<ConsultationDetailViewModel>();
        builder.Services.AddTransient<PrescriptionPage>().AddTransient<PrescriptionViewModel>();
        builder.Services.AddTransient<PrescriptionSharePage>().AddTransient<PrescriptionShareViewModel>();
        builder.Services.AddTransient<InvoicesPage>().AddTransient<InvoicesViewModel>();
        builder.Services.AddTransient<InvoiceDetailPage>().AddTransient<InvoiceDetailViewModel>();
        builder.Services.AddTransient<PaymentPage>().AddTransient<PaymentViewModel>();
        builder.Services.AddTransient<SearchPage>().AddTransient<SearchViewModel>();
        builder.Services.AddTransient<SettingsPage>().AddTransient<SettingsViewModel>();

        return builder.Build();
    }
}
