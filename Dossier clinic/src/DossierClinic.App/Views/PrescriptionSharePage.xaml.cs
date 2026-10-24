using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class PrescriptionSharePage : ContentPage
{
    readonly PrescriptionShareViewModel _vm;
    public PrescriptionSharePage(PrescriptionShareViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
}
