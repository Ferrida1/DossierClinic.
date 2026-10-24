using System.Collections.ObjectModel;
using DossierClinic.Shared;

namespace DossierClinic.Services;

/// <summary>Holds the prescription being typed while a new consultation has not been saved yet.</summary>
public class PrescriptionDraftStore
{
    public ObservableCollection<PrescriptionLine> Lines { get; } = new();
}
