using DossierClinic.Services;
using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class SearchPage : ContentPage
{
    readonly SearchViewModel _vm;
    public SearchPage(SearchViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    void OnConsultationTapped(object? sender, TappedEventArgs e)
    {
        if (((BindableObject)sender!).BindingContext is ConsultationRow r) _vm.OpenConsultationCommand.Execute(r);
    }
    void OnPatientTapped(object? sender, TappedEventArgs e)
    {
        if (((BindableObject)sender!).BindingContext is PatientRow r) _vm.OpenPatientCommand.Execute(r);
    }
}
