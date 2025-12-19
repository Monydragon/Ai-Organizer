using CommunityToolkit.Mvvm.ComponentModel;

namespace Ai_Organizer.ViewModels;

public sealed partial class PlanPreviewViewModel : ObservableObject
{
    [ObservableProperty]
    private string _placeholder = "No plan yet. Run a scan, then analyze to generate a plan.";
}


