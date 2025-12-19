using CommunityToolkit.Mvvm.ComponentModel;

namespace Ai_Organizer.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        ScanViewModel scan,
        PlanPreviewViewModel planPreview,
        SettingsViewModel settings)
    {
        Scan = scan;
        PlanPreview = planPreview;
        Settings = settings;
    }

    public ScanViewModel Scan { get; }
    public PlanPreviewViewModel PlanPreview { get; }
    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private string _statusText = "Ready.";
}


