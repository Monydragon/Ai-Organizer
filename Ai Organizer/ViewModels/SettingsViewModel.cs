using Ai_Organizer.Models.Settings;
using Ai_Organizer.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettingsService _settingsService;

    public SettingsViewModel(AppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [ObservableProperty]
    private string _ollamaEndpoint = "http://localhost:11434";

    [ObservableProperty]
    private string _ollamaModel = "llama3.2";

    [ObservableProperty]
    private string _openAiEndpoint = "https://api.openai.com/v1";

    [ObservableProperty]
    private string _openAiModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _defaultPrompt = "";

    [ObservableProperty]
    private string _status = "Settings not loaded yet.";

    [RelayCommand]
    private async Task LoadAsync()
    {
        var settings = await _settingsService.GetAsync(CancellationToken.None);
        OllamaEndpoint = settings.Ollama.Endpoint;
        OllamaModel = settings.Ollama.Model;
        OpenAiEndpoint = settings.OpenAi.Endpoint;
        OpenAiModel = settings.OpenAi.Model;
        DefaultPrompt = settings.DefaultPrompt;
        Status = "Loaded.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = await _settingsService.GetAsync(CancellationToken.None);
        settings.Ollama.Endpoint = OllamaEndpoint;
        settings.Ollama.Model = OllamaModel;
        settings.OpenAi.Endpoint = OpenAiEndpoint;
        settings.OpenAi.Model = OpenAiModel;
        settings.DefaultPrompt = DefaultPrompt;
        await _settingsService.SaveAsync(settings, CancellationToken.None);
        Status = "Saved.";
    }
}


