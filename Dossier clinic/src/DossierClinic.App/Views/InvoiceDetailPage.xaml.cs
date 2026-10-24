using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class InvoiceDetailPage : ContentPage
{
    readonly InvoiceDetailViewModel _vm;
    public InvoiceDetailPage(InvoiceDetailViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
}
