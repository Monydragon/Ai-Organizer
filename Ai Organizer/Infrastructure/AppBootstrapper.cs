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
        services.AddSingleton<INaturalLanguageOrganizationService, NaturalLanguageOrganizationService>();
        services.AddHttpClient();
        
        // LLM services - register providers as both concrete types and interfaces
        services.AddSingleton<OllamaProvider>();
        services.AddSingleton<IModelRepository>(sp => sp.GetRequiredService<OllamaProvider>());
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<OllamaProvider>());
        
        services.AddSingleton<OpenAiProvider>();
        services.AddSingleton<IModelRepository>(sp => sp.GetRequiredService<OpenAiProvider>());
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<OpenAiProvider>());
        
        services.AddSingleton<HuggingFaceRepository>();
        services.AddSingleton<IModelRepository>(sp => sp.GetRequiredService<HuggingFaceRepository>());
        
        services.AddSingleton<GoogleGeminiProvider>();
        services.AddSingleton<IModelRepository>(sp => sp.GetRequiredService<GoogleGeminiProvider>());
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<GoogleGeminiProvider>());
        
        services.AddSingleton<AnthropicProvider>();
        services.AddSingleton<IModelRepository>(sp => sp.GetRequiredService<AnthropicProvider>());
        services.AddSingleton<ILLMProvider>(sp => sp.GetRequiredService<AnthropicProvider>());
        
        // Model evaluation and browser services
        services.AddSingleton<IModelEvaluationService, ModelEvaluationService>();

        // Register concrete + interface mapping to ensure a single shared instance
        services.AddSingleton<ModelBrowser>();
        services.AddSingleton<IModelBrowser>(sp => sp.GetRequiredService<ModelBrowser>());

        services.AddSingleton<ModelRepositoryService>();
        services.AddSingleton<OllamaDockerService>();

        // View-models
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ScanViewModel>();
        services.AddSingleton<PlanPreviewViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ModelBrowserViewModel>();
        services.AddSingleton<InteractiveConfigViewModel>();

        // Views / windows
        services.AddSingleton<MainWindow>();

        var provider = services.BuildServiceProvider();

        // Fail fast with a clear message if a critical service isn't wired.
        _ = provider.GetRequiredService<IModelBrowser>();
        _ = provider.GetRequiredService<INaturalLanguageOrganizationService>();

        return provider;
    }
}
