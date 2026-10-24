using DossierClinic.Services;
using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class InvoicesPage : ContentPage
{
    readonly InvoicesViewModel _vm;
    public InvoicesPage(InvoicesViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
    void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if (((BindableObject)sender!).BindingContext is InvoiceRow row) _vm.OpenCommand.Execute(row);
    }
}
