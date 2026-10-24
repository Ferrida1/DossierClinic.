using DossierClinic.Services;
using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class PatientDetailPage : ContentPage
{
    readonly PatientDetailViewModel _vm;
    public PatientDetailPage(PatientDetailViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
    void OnHistoryTapped(object? sender, TappedEventArgs e)
    {
        if (((BindableObject)sender!).BindingContext is HistoryRow row) _vm.OpenConsultationCommand.Execute(row);
    }
}
