using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class PatientFormPage : ContentPage
{
    readonly PatientFormViewModel _vm;
    public PatientFormPage(PatientFormViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
}
