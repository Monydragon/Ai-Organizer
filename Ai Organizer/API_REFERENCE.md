# Developer API Reference

## Complete Service Architecture

### Model Management Services

#### IModelBrowser
```csharp
namespace Ai_Organizer.Services.Llm;

public interface IModelBrowser
{
    /// <summary>
    /// Get all available models from all enabled providers with performance data.
    /// </summary>
    Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get only downloaded/registered models.
    /// </summary>
    Task<IReadOnlyList<ModelInfo>> GetDownloadedModelsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Download a model with progress reporting.
    /// </summary>
    /// <param name="modelName">Model identifier</param>
    /// <param name="providerName">Provider name (Ollama, OpenAI, etc.)</param>
    /// <param name="progress">Optional progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if download succeeded</returns>
    Task<bool> DownloadModelAsync(
        string modelName, 
        string providerName, 
        IProgress<ModelDownloadProgress>? progress, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Delete a model and clear its metrics.
    /// </summary>
    Task<bool> DeleteModelAsync(string modelName, string providerName, CancellationToken cancellationToken);

    /// <summary>
    /// Update a model to latest version.
    /// </summary>
    Task<bool> UpdateModelAsync(
        string modelName, 
        string providerName, 
        IProgress<ModelDownloadProgress>? progress, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Get detailed information about a specific model.
    /// </summary>
    Task<ModelInfo?> GetModelInfoAsync(string modelName, string providerName, CancellationToken cancellationToken);
}
```

**Usage Example**:
```csharp
var modelBrowser = serviceProvider.GetRequiredService<IModelBrowser>();
var allModels = await modelBrowser.GetAvailableModelsAsync(cancellationToken);

var progress = new Progress<ModelDownloadProgress>(p =>
{
    Console.WriteLine($"{p.ModelName}: {p.ProgressPercent:P}");
});

var success = await modelBrowser.DownloadModelAsync(
    "llama3.2", 
    "Ollama", 
    progress, 
    cancellationToken
);
```

---

#### IModelEvaluationService
```csharp
namespace Ai_Organizer.Services.Llm;

public interface IModelEvaluationService
{
    /// <summary>
    /// Record metrics for a completed LLM request.
    /// </summary>
    Task RecordMetricsAsync(ModelPerformanceMetrics metrics, CancellationToken cancellationToken);

    /// <summary>
    /// Get aggregated performance summary for one model.
    /// </summary>
    /// <returns>Null if no metrics exist</returns>
    Task<ModelPerformanceSummary?> GetSummaryAsync(
        string modelName, 
        string providerName, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Get performance summaries for all models, ranked by quality.
    /// </summary>
    Task<IReadOnlyList<ModelPerformanceSummary>> GetAllSummariesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get detailed metrics history for one model.
    /// </summary>
    /// <returns>List ordered by most recent first</returns>
    Task<IReadOnlyList<ModelPerformanceMetrics>> GetMetricsAsync(
        string modelName, 
        string providerName, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Clear all metrics for a specific model.
    /// </summary>
    Task ClearMetricsAsync(string modelName, string providerName, CancellationToken cancellationToken);
}
```

**Usage Example**:
```csharp
var evaluationService = serviceProvider.GetRequiredService<IModelEvaluationService>();

// Record a metric after LLM call
var stopwatch = Stopwatch.StartNew();
var response = await llmProvider.ChatJsonAsync(request, cancellationToken);
stopwatch.Stop();

var metrics = new ModelPerformanceMetrics
{
    ModelName = "llama3.2",
    ProviderName = "Ollama",
    Timestamp = DateTime.UtcNow,
    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
    QualityScore = 0.85,
    TaskType = "FileOrganization",
    Success = true
};

await evaluationService.RecordMetricsAsync(metrics, cancellationToken);

// Get summary
var summary = await evaluationService.GetSummaryAsync("llama3.2", "Ollama", cancellationToken);
Console.WriteLine($"Avg Quality: {summary?.AverageQualityScore:P}");
Console.WriteLine($"Avg Latency: {summary?.AverageResponseTimeMs}ms");
```

---

### Organization Configuration Services

#### INaturalLanguageOrganizationService
```csharp
namespace Ai_Organizer.Services.Organizing;

public interface INaturalLanguageOrganizationService
{
    /// <summary>
    /// Parse natural language user input into strategy and constraints.
    /// </summary>
    /// <param name="userInput">User's spoken/written instructions</param>
    /// <returns>
    /// Tuple of:
    /// - strategy: Parsed organization method
    /// - constraints: Extracted rules
    /// </returns>
    Task<(string strategy, List<string> constraints)> ParseInstructionsAsync(
        string userInput,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generate a system prompt for LLM based on configuration.
    /// </summary>
    /// <remarks>
    /// This prompt includes the organization strategy, constraints, rules,
    /// metadata preferences, and file type exclusions.
    /// Ready to use as LLM system instruction.
    /// </remarks>
    string GenerateSystemPromptFromConfig(OrganizationConfiguration config);
}
```

**Usage Example**:
```csharp
var nlService = serviceProvider.GetRequiredService<INaturalLanguageOrganizationService>();

var userInput = "Group files by type, then organize by date. Keep files under 2GB per folder.";
var (strategy, constraints) = await nlService.ParseInstructionsAsync(userInput, cancellationToken);

Console.WriteLine($"Strategy: {strategy}");
foreach (var constraint in constraints)
    Console.WriteLine($"  - {constraint}");

// Generate system prompt
var config = new OrganizationConfiguration
{
    OrganizationStrategy = strategy,
    Constraints = constraints,
    UpdateMetadata = true,
    MaxFolderDepth = 5
};

var systemPrompt = nlService.GenerateSystemPromptFromConfig(config);
Console.WriteLine(systemPrompt);
```

---

## Data Models

### ModelPerformanceMetrics
```csharp
public sealed class ModelPerformanceMetrics
{
    public string ModelName { get; set; }              // "llama3.2"
    public string ProviderName { get; set; }           // "Ollama"
    public DateTime Timestamp { get; set; }            // When the request happened
    
    public long ResponseTimeMs { get; set; }           // How long it took
    public double QualityScore { get; set; }           // 0.0 to 1.0 rating
    public decimal CostUsd { get; set; }               // Dollar amount if API-based
    
    public int? InputTokens { get; set; }              // For token-based models
    public int? OutputTokens { get; set; }
    
    public string TaskType { get; set; }               // "FileOrganization", etc.
    public string? Details { get; set; }               // Optional metadata
    
    public bool Success { get; set; }                  // Did it work?
    public string? ErrorMessage { get; set; }          // If failed, why?
}
```

### ModelPerformanceSummary
```csharp
public sealed class ModelPerformanceSummary
{
    public string ModelName { get; set; }
    public string ProviderName { get; set; }
    
    public int TotalRequests { get; set; }             // Total times used
    public int SuccessfulRequests { get; set; }        // Successful times
    public double AverageResponseTimeMs { get; set; }  // Avg latency
    public double AverageQualityScore { get; set; }    // Avg quality 0-1
    public decimal TotalCostUsd { get; set; }          // Total spent
    
    public DateTime FirstUsed { get; set; }            // First request
    public DateTime LastUsed { get; set; }             // Most recent request
}
```

### ModelInfo
```csharp
public sealed class ModelInfo
{
    public string Name { get; set; }                   // "llama3.2"
    public string ProviderName { get; set; }           // "Ollama"
    public RepositoryType ProviderType { get; set; }
    
    public bool SupportsVision { get; set; }           // Can analyze images?
    public bool IsDownloaded { get; set; }             // Downloaded locally?
    public DateTime? DateDownloaded { get; set; }      // When downloaded
    public long? SizeBytes { get; set; }               // Model size
    
    public string? Description { get; set; }           // Model description
    public double? PerformanceScore { get; set; }      // 0-1 from evaluation service
}
```

### OrganizationConfiguration
```csharp
public sealed class OrganizationConfiguration
{
    // User's organization method
    public string OrganizationStrategy { get; set; }
    
    // 0 = fully automatic, 1 = ask for everything
    public double Handiness { get; set; }
    
    // Rules to enforce
    public List<string> Constraints { get; set; }
    public List<string> StandardRules { get; set; }
    
    // Metadata handling
    public bool UpdateMetadata { get; set; }
    public bool PreserveExistingMetadata { get; set; }
    
    // File filtering
    public string ExcludeFileTypes { get; set; }       // "exe,dll,sys"
    
    // Organization limits
    public int MaxFolderDepth { get; set; }
    public bool LiveUpdateDuringScan { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## LLM Provider Implementations

### ILLMProvider Interface
```csharp
public interface ILLMProvider
{
    string Name { get; }                    // "Ollama", "OpenAI", "Google Gemini", "Anthropic"
    bool SupportsVision { get; }            // Can analyze images?

    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken);
    
    Task<string> ChatJsonAsync(ChatJsonRequest request, CancellationToken cancellationToken);
}
```

### Available Implementations

#### OllamaProvider
- Models: llama3.2, mistral, neural-chat, etc.
- Endpoint: Configurable (default: `http://localhost:11434`)
- Authentication: None (local)
- Vision: Yes (model-dependent)

#### OpenAiProvider
- Models: gpt-4, gpt-4-turbo, gpt-3.5-turbo, etc.
- Endpoint: `https://api.openai.com/v1`
- Authentication: API key
- Vision: Yes

#### GoogleGeminiProvider
- Models: gemini-pro, gemini-1.5-pro, etc.
- Endpoint: `https://generativelanguage.googleapis.com/v1beta`
- Authentication: API key
- Vision: Yes

#### AnthropicProvider
- Models: claude-3-opus, claude-3-sonnet, claude-3-haiku
- Endpoint: `https://api.anthropic.com/v1`
- Authentication: API key
- Vision: Yes

#### HuggingFaceRepository
- Models: Thousands available on HuggingFace Hub
- Endpoint: `https://huggingface.co/api`
- Authentication: Optional token
- Vision: Model-dependent

---

## View Models

### ModelBrowserViewModel
```csharp
public sealed partial class ModelBrowserViewModel : ObservableObject
{
    // Collections
    public ObservableCollection<ModelInfo> AvailableModels { get; }
    public ObservableCollection<ModelInfo> DownloadedModels { get; }
    public ObservableCollection<ModelPerformanceSummary> ModelPerformance { get; }

    // Properties
    [ObservableProperty] ModelInfo? SelectedModel;
    [ObservableProperty] bool IsLoading;
    [ObservableProperty] string StatusMessage;
    [ObservableProperty] double DownloadProgress;

    // Commands
    public AsyncRelayCommand LoadModelsCommand { get; }
    public AsyncRelayCommand<ModelInfo> DownloadModelCommand { get; }
    public AsyncRelayCommand<ModelInfo> DeleteModelCommand { get; }
    public AsyncRelayCommand<ModelInfo> UpdateModelCommand { get; }
    public RelayCommand CancelOperationCommand { get; }
}
```

### InteractiveConfigViewModel
```csharp
public sealed partial class InteractiveConfigViewModel : ObservableObject
{
    // Display
    public ObservableCollection<string> QuestionsAndAnswers { get; }

    // Properties
    [ObservableProperty] string CurrentQuestion;
    [ObservableProperty] string UserInput;
    [ObservableProperty] double HandinessLevel;           // 0-1 slider
    [ObservableProperty] bool UpdateMetadata;
    [ObservableProperty] bool PreserveExistingMetadata;
    [ObservableProperty] string ExcludeFileTypes;
    [ObservableProperty] int MaxFolderDepth;
    [ObservableProperty] bool IsComplete;
    [ObservableProperty] string StatusMessage;

    // Commands
    public AsyncRelayCommand SubmitAnswerCommand { get; }
    public RelayCommand ResetCommand { get; }

    // Method
    public OrganizationConfiguration GetConfiguration();
}
```

---

## Integration Examples

### Recording Model Performance After LLM Call
```csharp
public async Task<string> ExecuteWithMetricsAsync(
    string model,
    string provider,
    ChatJsonRequest request,
    CancellationToken cancellationToken)
{
    var sw = Stopwatch.StartNew();
    
    try
    {
        var response = await _llmProvider.ChatJsonAsync(request, cancellationToken);
        sw.Stop();
        
        var metrics = new ModelPerformanceMetrics
        {
            ModelName = model,
            ProviderName = provider,
            Timestamp = DateTime.UtcNow,
            ResponseTimeMs = sw.ElapsedMilliseconds,
            QualityScore = 0.9,  // Placeholder - rate based on results
            TaskType = "FileOrganization",
            Success = true
        };
        
        await _evaluationService.RecordMetricsAsync(metrics, cancellationToken);
        return response;
    }
    catch (Exception ex)
    {
        sw.Stop();
        
        var failureMetrics = new ModelPerformanceMetrics
        {
            ModelName = model,
            ProviderName = provider,
            Timestamp = DateTime.UtcNow,
            ResponseTimeMs = sw.ElapsedMilliseconds,
            Success = false,
            ErrorMessage = ex.Message
        };
        
        await _evaluationService.RecordMetricsAsync(failureMetrics, cancellationToken);
        throw;
    }
}
```

### Using Organization Configuration in Planning
```csharp
public async Task<OrganizationPlan> CreatePlanAsync(
    IReadOnlyList<FileContext> files,
    OrganizationConfiguration config,
    CancellationToken cancellationToken)
{
    // Generate system prompt from configuration
    var systemPrompt = _nlService.GenerateSystemPromptFromConfig(config);
    
    // Call LLM with the configuration-based prompt
    var request = new ChatJsonRequest
    {
        Model = _settings.PreferredProvider,
        SystemPrompt = systemPrompt,
        UserPrompt = _promptTemplates.BuildPlannerUserPrompt("MyFiles", files)
    };
    
    var response = await _llmProvider.ChatJsonAsync(request, cancellationToken);
    
    // Parse response into plan
    var plan = JsonSerializer.Deserialize<OrganizationPlan>(response);
    
    // Record metrics
    await _evaluationService.RecordMetricsAsync(
        new ModelPerformanceMetrics
        {
            ModelName = request.Model,
            ProviderName = _settings.PreferredProvider,
            Timestamp = DateTime.UtcNow,
            QualityScore = 0.85,
            TaskType = "FileOrganization"
        },
        cancellationToken
    );
    
    return plan;
}
```

---

## Dependency Injection Setup

```csharp
public static ServiceProvider BuildServices()
{
    var services = new ServiceCollection();

    // Settings and persistence
    services.AddSingleton<IAppSettingsStore, JsonAppSettingsStore>();
    services.AddSingleton<AppSettingsService>();
    services.AddSingleton<ISecretStore, FileSecretStore>();

    // LLM providers
    services.AddSingleton<OllamaProvider>();
    services.AddSingleton<OpenAiProvider>();
    services.AddSingleton<GoogleGeminiProvider>();
    services.AddSingleton<AnthropicProvider>();
    services.AddSingleton<HuggingFaceRepository>();
    services.AddHttpClient();

    // Services
    services.AddSingleton<ModelRepositoryService>();
    services.AddSingleton<OllamaDockerService>();
    services.AddSingleton<IModelEvaluationService, ModelEvaluationService>();
    services.AddSingleton<IModelBrowser, ModelBrowser>();
    services.AddSingleton<INaturalLanguageOrganizationService, NaturalLanguageOrganizationService>();
    
    // UI
    services.AddSingleton<IFilePickerService, FilePickerService>();
    
    // View models
    services.AddSingleton<MainWindowViewModel>();
    services.AddSingleton<ModelBrowserViewModel>();
    services.AddSingleton<InteractiveConfigViewModel>();

    return services.BuildServiceProvider();
}
```

---

## Configuration Storage

### AppSettings Location
```
%APPDATA%\Ai-Organizer\settings.json
```

### API Keys Storage
```
%APPDATA%\Ai-Organizer\secrets\
```
Keys are encrypted using Windows Data Protection API (DPAPI).

### Model Metrics
```
%APPDATA%\Ai-Organizer\model_metrics.json
```

---

**Last Updated**: December 2024
**Version**: 1.0.0

