using DossierClinic.Services;
using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class PatientsPage : ContentPage
{
    readonly PatientsViewModel _vm;
    public PatientsPage(PatientsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
    void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if (((BindableObject)sender!).BindingContext is PatientRow row) _vm.OpenCommand.Execute(row);
    }
}
