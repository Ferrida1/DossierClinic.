using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class ConsultationFormPage : ContentPage
{
    readonly ConsultationFormViewModel _vm;
    public ConsultationFormPage(ConsultationFormViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    // Reloading on every appearance is safe: the VM only loads patient data once and just refreshes the prescription summary.
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
}
