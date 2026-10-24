using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class ConsultationDetailPage : ContentPage
{
    readonly ConsultationDetailViewModel _vm;
    public ConsultationDetailPage(ConsultationDetailViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
}
