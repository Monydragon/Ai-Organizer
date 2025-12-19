using CommunityToolkit.Mvvm.ComponentModel;

namespace Ai_Organizer.ViewModels;

public sealed partial class ScanViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedRootsSummary = "No roots selected yet.";

    [ObservableProperty]
    private bool _includeHidden = false;

    [ObservableProperty]
    private int _maxDepth = 12;

    [ObservableProperty]
    private string _includeGlob = "**/*";

    [ObservableProperty]
    private string _excludeGlob = "**/{bin,obj}/**";
}


