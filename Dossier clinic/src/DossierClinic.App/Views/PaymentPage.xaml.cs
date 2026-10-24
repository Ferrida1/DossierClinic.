using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class PaymentPage : ContentPage
{
    readonly PaymentViewModel _vm;
    public PaymentPage(PaymentViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
}
