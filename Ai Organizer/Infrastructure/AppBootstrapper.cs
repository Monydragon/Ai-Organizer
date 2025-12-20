using Ai_Organizer.Services.Settings;
using Ai_Organizer.Services.Scanning;
using Ai_Organizer.Services.Extraction;
using Ai_Organizer.Services.Ui;
using Ai_Organizer.Services.Organizing;
using Ai_Organizer.Services.Llm;
using Ai_Organizer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Ai_Organizer.Infrastructure;

public static class AppBootstrapper
{
    public static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        // Settings + persistence
        services.AddSingleton<IAppSettingsStore, JsonAppSettingsStore>();
        services.AddSingleton<AppSettingsService>();
        services.AddSingleton<ISecretStore, FileSecretStore>();

        // UI services
        services.AddSingleton<IFilePickerService, FilePickerService>();

        // Domain services
        services.AddSingleton<FileScanner>();
        services.AddSingleton<IFileContextEnricher, MetadataEnricher>();
        services.AddSingleton<IFileContextEnricher, TextEnricher>();
        services.AddSingleton<IFileContextEnricher, ImageThumbnailEnricher>();
        services.AddSingleton<FileContextBuilder>();
        services.AddSingleton<PlanValidator>();
        services.AddHttpClient();
        services.AddSingleton<OllamaProvider>();
        services.AddSingleton<OpenAiProvider>();

        // View-models
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ScanViewModel>();
        services.AddSingleton<PlanPreviewViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Views / windows
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}


