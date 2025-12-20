using CommunityToolkit.Mvvm.ComponentModel;

namespace Ai_Organizer.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        ScanViewModel scan,
        PlanPreviewViewModel planPreview,
        SettingsViewModel settings,
        ModelBrowserViewModel modelBrowser,
        InteractiveConfigViewModel interactiveConfig)
    {
        Scan = scan;
        PlanPreview = planPreview;
        Settings = settings;
        ModelBrowser = modelBrowser;
        InteractiveConfig = interactiveConfig;

        // Fire-and-forget initial load so the Models tab isn't empty.
        _ = ModelBrowser.LoadModelsAsync();
    }

    public ScanViewModel Scan { get; }
    public PlanPreviewViewModel PlanPreview { get; }
    public SettingsViewModel Settings { get; }
    public ModelBrowserViewModel ModelBrowser { get; }
    public InteractiveConfigViewModel InteractiveConfig { get; }

    [ObservableProperty]
    private string _statusText = "Ready.";
}
