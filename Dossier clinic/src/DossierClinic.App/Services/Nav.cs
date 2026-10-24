namespace DossierClinic.Services;

public static class Nav
{
    public static Task GoAsync(string route, params (string Key, string Value)[] query)
    {
        var q = query.Length == 0 ? "" : "?" + string.Join("&", query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return Shell.Current.GoToAsync(route + q);
    }
    public static Task BackAsync() => Shell.Current.GoToAsync("..");
    public static Task AlertAsync(string title, string message) => Shell.Current.DisplayAlert(title, message, "OK");
    public static Task<bool> ConfirmAsync(string title, string message, string accept = "Oui") =>
        Shell.Current.DisplayAlert(title, message, accept, "Annuler");

    public static class Routes
    {
        public const string Patient = "patient";
        public const string PatientForm = "patientform";
        public const string ConsultationForm = "consultationform";
        public const string Consultation = "consultation";
        public const string Prescription = "prescription";
        public const string PrescriptionShare = "prescriptionshare";
        public const string Invoice = "invoice";
        public const string Payment = "payment";
        public const string Settings = "settings";
    }
}
