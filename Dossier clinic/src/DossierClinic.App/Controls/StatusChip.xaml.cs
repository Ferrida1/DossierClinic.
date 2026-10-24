using DossierClinic.Services;

namespace DossierClinic.Controls;

public partial class StatusChip : Border
{
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status), typeof(StatusStyle), typeof(StatusChip), null,
        propertyChanged: (b, _, n) => ((StatusChip)b).Apply(n as StatusStyle));

    public StatusStyle? Status { get => (StatusStyle?)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    public StatusChip() => InitializeComponent();

    void Apply(StatusStyle? s)
    {
        if (s is null) return;
        BackgroundColor = s.Bg; Caption.TextColor = s.Fg; Caption.Text = s.Text;
    }
}
