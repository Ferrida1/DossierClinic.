using DossierClinic.Shared;
using DossierClinic.ViewModels;

namespace DossierClinic.Views;

public partial class PrescriptionPage : ContentPage
{
    readonly PrescriptionViewModel _vm;
    public PrescriptionPage(PrescriptionViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
    void OnRemoveTapped(object? sender, TappedEventArgs e)
    {
        if (((BindableObject)sender!).BindingContext is PrescriptionLine line) _vm.RemoveCommand.Execute(line);
    }
}
