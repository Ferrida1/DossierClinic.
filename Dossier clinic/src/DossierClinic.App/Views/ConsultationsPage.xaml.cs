using DossierClinic.Services;
using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class ConsultationsPage : ContentPage
{
    readonly ConsultationsViewModel _vm;
    public ConsultationsPage(ConsultationsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
    void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if (((BindableObject)sender!).BindingContext is ConsultationRow row) _vm.OpenCommand.Execute(row);
    }
}
