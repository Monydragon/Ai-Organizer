using Ai_Organizer.Models.Settings;
using Ai_Organizer.Services.Llm;
using Ai_Organizer.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettingsService _settingsService;
    private readonly ISecretStore _secrets;
    private readonly OllamaProvider _ollama;
    private readonly OpenAiProvider _openAi;

    public SettingsViewModel(
        AppSettingsService settingsService,
        ISecretStore secrets,
        OllamaProvider ollama,
        OpenAiProvider openAi)
    {
        _settingsService = settingsService;
        _secrets = secrets;
        _ollama = ollama;
        _openAi = openAi;
    }

    public ObservableCollection<string> OllamaModels { get; } = new();
    public ObservableCollection<string> OpenAiModels { get; } = new();

    public ObservableCollection<string> ProviderOptions { get; } = new() { "Ollama", "OpenAI" };

    [ObservableProperty]
    private string _preferredProvider = "Ollama";

    [ObservableProperty]
    private string _ollamaEndpoint = "http://localhost:11434";

    [ObservableProperty]
    private string _ollamaModel = "llama3.2";

    [ObservableProperty]
    private string _openAiEndpoint = "https://api.openai.com/v1";

    [ObservableProperty]
    private string _openAiModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _openAiApiKey = "";

    [ObservableProperty]
    private bool _hasOpenAiApiKey;

    [ObservableProperty]
    private string _defaultPrompt = "";

    [ObservableProperty]
    private string _status = "Settings not loaded yet.";

    [RelayCommand]
    private async Task LoadAsync()
    {
        var settings = await _settingsService.GetAsync(CancellationToken.None);
        PreferredProvider = settings.PreferredProvider;
        OllamaEndpoint = settings.Ollama.Endpoint;
        OllamaModel = settings.Ollama.Model;
        OpenAiEndpoint = settings.OpenAi.Endpoint;
        OpenAiModel = settings.OpenAi.Model;
        DefaultPrompt = settings.DefaultPrompt;

        HasOpenAiApiKey = !string.IsNullOrWhiteSpace(await _secrets.GetAsync(SecretKeys.OpenAiApiKey, CancellationToken.None));
        OpenAiApiKey = "";
        Status = "Loaded.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = await _settingsService.GetAsync(CancellationToken.None);
        settings.PreferredProvider = PreferredProvider;
        settings.Ollama.Endpoint = OllamaEndpoint;
        settings.Ollama.Model = OllamaModel;
        settings.OpenAi.Endpoint = OpenAiEndpoint;
        settings.OpenAi.Model = OpenAiModel;
        settings.DefaultPrompt = DefaultPrompt;

        if (!string.IsNullOrWhiteSpace(OpenAiApiKey))
        {
            await _secrets.SetAsync(SecretKeys.OpenAiApiKey, OpenAiApiKey.Trim(), CancellationToken.None);
            HasOpenAiApiKey = true;
            OpenAiApiKey = "";
        }

        await _settingsService.SaveAsync(settings, CancellationToken.None);
        Status = "Saved.";
    }

    [RelayCommand]
    private async Task TestOllamaAsync()
    {
        Status = "Testing Ollama...";
        OllamaModels.Clear();
        try
        {
            var models = await _ollama.ListModelsAsync(CancellationToken.None);
            foreach (var m in models)
                OllamaModels.Add(m);
            Status = models.Count == 0 ? "Ollama reachable, but no models found." : $"Ollama OK. {models.Count} model(s).";
        }
        catch (System.Exception ex)
        {
            Status = $"Ollama test failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestOpenAiAsync()
    {
        Status = "Testing OpenAI...";
        OpenAiModels.Clear();
        try
        {
            var models = await _openAi.ListModelsAsync(CancellationToken.None);
            foreach (var m in models.Take(200))
                OpenAiModels.Add(m);
            Status = models.Count == 0 ? "OpenAI OK (or key missing). No models returned." : $"OpenAI OK. {models.Count} model(s).";
        }
        catch (System.Exception ex)
        {
            Status = $"OpenAI test failed: {ex.Message}";
        }
    }
}


