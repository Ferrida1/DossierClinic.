using DossierClinic.Services;
using DossierClinic.Views;

namespace DossierClinic;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Nav.Routes.Patient, typeof(PatientDetailPage));
        Routing.RegisterRoute(Nav.Routes.PatientForm, typeof(PatientFormPage));
        Routing.RegisterRoute(Nav.Routes.ConsultationForm, typeof(ConsultationFormPage));
        Routing.RegisterRoute(Nav.Routes.Consultation, typeof(ConsultationDetailPage));
        Routing.RegisterRoute(Nav.Routes.Prescription, typeof(PrescriptionPage));
        Routing.RegisterRoute(Nav.Routes.PrescriptionShare, typeof(PrescriptionSharePage));
        Routing.RegisterRoute(Nav.Routes.Invoice, typeof(InvoiceDetailPage));
        Routing.RegisterRoute(Nav.Routes.Payment, typeof(PaymentPage));
        Routing.RegisterRoute(Nav.Routes.Settings, typeof(SettingsPage));
    }
}
