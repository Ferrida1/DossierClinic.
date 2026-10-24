using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class SettingsPage : ContentPage
{
    readonly SettingsViewModel _vm;
    public SettingsPage(SettingsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
    protected override async void OnDisappearing() { base.OnDisappearing(); await _vm.SaveAsync(); }
}
