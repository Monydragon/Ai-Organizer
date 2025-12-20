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
    private readonly HuggingFaceRepository _huggingFace;
    private readonly OllamaDockerService _ollamaDocker;

    public SettingsViewModel(
        AppSettingsService settingsService,
        ISecretStore secrets,
        OllamaProvider ollama,
        OpenAiProvider openAi,
        HuggingFaceRepository huggingFace,
        OllamaDockerService ollamaDocker)
    {
        _settingsService = settingsService;
        _secrets = secrets;
        _ollama = ollama;
        _openAi = openAi;
        _huggingFace = huggingFace;
        _ollamaDocker = ollamaDocker;
    }

    public ObservableCollection<string> OllamaModels { get; } = new();
    public ObservableCollection<string> OpenAiModels { get; } = new();
    public ObservableCollection<string> HuggingFaceModels { get; } = new();

    public ObservableCollection<string> ProviderOptions { get; } = new() { "Ollama", "OpenAI", "Hugging Face" };

    [ObservableProperty]
    private string _preferredProvider = "Ollama";

    [ObservableProperty]
    private string _ollamaEndpoint = "http://localhost:11434";

    [ObservableProperty]
    private string _ollamaModel = "llama3.2";

    [ObservableProperty]
    private bool _ollamaUseDocker;

    [ObservableProperty]
    private string _ollamaDockerImage = "ollama/ollama:latest";

    [ObservableProperty]
    private string _ollamaDockerContainerName = "ai-organizer-ollama";

    [ObservableProperty]
    private int _ollamaDockerPort = 11434;

    [ObservableProperty]
    private string _openAiEndpoint = "https://api.openai.com/v1";

    [ObservableProperty]
    private string _openAiModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _openAiApiKey = "";

    [ObservableProperty]
    private bool _hasOpenAiApiKey;

    [ObservableProperty]
    private string _huggingFaceEndpoint = "https://huggingface.co/api/models";

    [ObservableProperty]
    private string _huggingFaceModel = "";

    [ObservableProperty]
    private bool _huggingFaceEnabled = true;

    [ObservableProperty]
    private string _huggingFaceToken = "";

    [ObservableProperty]
    private bool _hasHuggingFaceToken;

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
        OllamaUseDocker = settings.Ollama.UseDocker;
        OllamaDockerImage = settings.Ollama.DockerImage;
        OllamaDockerContainerName = settings.Ollama.DockerContainerName;
        OllamaDockerPort = settings.Ollama.DockerPort;
        OpenAiEndpoint = settings.OpenAi.Endpoint;
        OpenAiModel = settings.OpenAi.Model;
        HuggingFaceEndpoint = settings.HuggingFace.Endpoint;
        HuggingFaceModel = settings.HuggingFace.Model;
        HuggingFaceEnabled = settings.HuggingFace.Enabled;
        DefaultPrompt = settings.DefaultPrompt;

        HasOpenAiApiKey = !string.IsNullOrWhiteSpace(await _secrets.GetAsync(SecretKeys.OpenAiApiKey, CancellationToken.None));
        OpenAiApiKey = "";
        HasHuggingFaceToken = !string.IsNullOrWhiteSpace(await _secrets.GetAsync(SecretKeys.HuggingFaceToken, CancellationToken.None));
        HuggingFaceToken = "";
        Status = "Loaded.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = await _settingsService.GetAsync(CancellationToken.None);
        settings.PreferredProvider = PreferredProvider;
        settings.Ollama.Endpoint = OllamaEndpoint;
        settings.Ollama.Model = OllamaModel;
        settings.Ollama.UseDocker = OllamaUseDocker;
        settings.Ollama.DockerImage = OllamaDockerImage;
        settings.Ollama.DockerContainerName = OllamaDockerContainerName;
        settings.Ollama.DockerPort = OllamaDockerPort;
        settings.OpenAi.Endpoint = OpenAiEndpoint;
        settings.OpenAi.Model = OpenAiModel;
        settings.HuggingFace.Endpoint = HuggingFaceEndpoint;
        settings.HuggingFace.Model = HuggingFaceModel;
        settings.HuggingFace.Enabled = HuggingFaceEnabled;
        settings.DefaultPrompt = DefaultPrompt;

        if (!string.IsNullOrWhiteSpace(OpenAiApiKey))
        {
            await _secrets.SetAsync(SecretKeys.OpenAiApiKey, OpenAiApiKey.Trim(), CancellationToken.None);
            HasOpenAiApiKey = true;
            OpenAiApiKey = "";
        }

        if (!string.IsNullOrWhiteSpace(HuggingFaceToken))
        {
            await _secrets.SetAsync(SecretKeys.HuggingFaceToken, HuggingFaceToken.Trim(), CancellationToken.None);
            HasHuggingFaceToken = true;
            HuggingFaceToken = "";
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

    [RelayCommand]
    private async Task TestHuggingFaceAsync()
    {
        Status = "Testing Hugging Face...";
        HuggingFaceModels.Clear();
        try
        {
            var models = await _huggingFace.ListModelsAsync(CancellationToken.None);
            foreach (var m in models)
                HuggingFaceModels.Add(m);
            Status = models.Count == 0 ? "Hugging Face reachable, but no models found." : $"Hugging Face OK. {models.Count} model(s).";
        }
        catch (System.Exception ex)
        {
            Status = $"Hugging Face test failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task StartOllamaDockerAsync()
    {
        Status = "Starting Ollama (Docker)...";
        try
        {
            await _ollamaDocker.EnsureStartedAsync(OllamaDockerImage, OllamaDockerContainerName, OllamaDockerPort, CancellationToken.None);
            Status = "Ollama container is running.";
        }
        catch (System.Exception ex)
        {
            Status = $"Failed to start Ollama container: {ex.Message}";
        }
    }
}
