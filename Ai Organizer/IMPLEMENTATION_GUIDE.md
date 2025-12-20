# AI Organizer - Full Implementation Guide

## Overview
This document describes the comprehensive implementation of model management, multi-provider support, interactive configuration, and performance tracking features.

## New Features

### 1. Model Browser (`ModelBrowserView`, `ModelBrowserViewModel`)
**Location**: `Views/ModelBrowserView.axaml` and `ViewModels/ModelBrowserViewModel.cs`

**Functionality**:
- Browse all available models across all providers (Ollama, OpenAI, Google Gemini, Anthropic, HuggingFace)
- Download models with progress tracking
- Delete downloaded models
- Update existing models
- View performance metrics for each model
- Real-time status updates

**Key Commands**:
- `LoadModelsCommand` - Refresh model list
- `DownloadModelCommand` - Download selected model
- `DeleteModelCommand` - Remove downloaded model
- `UpdateModelCommand` - Update to latest version
- `CancelOperationCommand` - Cancel ongoing operation

**Properties**:
- `AvailableModels` - All models from all providers
- `DownloadedModels` - Only downloaded models
- `ModelPerformance` - Performance rankings
- `SelectedModel` - Currently selected model
- `DownloadProgress` - Download progress (0-100%)

---

### 2. Multi-Provider Support

#### New Providers

##### Google Gemini (`GoogleGeminiProvider.cs`)
- API: `https://generativelanguage.googleapis.com/v1beta/`
- Models: gemini-pro, gemini-1.5-pro, etc.
- Supports vision capabilities
- API key stored securely via `SecretKeys.GoogleGeminiApiKey`

**Settings** (`GoogleGeminiSettings`):
```csharp
public string Model { get; set; } = "gemini-pro";
public bool HasApiKey { get; set; }
```

##### Anthropic (`AnthropicProvider.cs`)
- API: `https://api.anthropic.com/v1/`
- Models: claude-3-opus, claude-3-sonnet, claude-3-haiku
- Supports vision capabilities
- API key stored securely via `SecretKeys.AnthropicApiKey`

**Settings** (`AnthropicSettings`):
```csharp
public string Model { get; set; } = "claude-3-sonnet-20250219";
public bool HasApiKey { get; set; }
```

#### Provider Registration
All providers are registered in `AppBootstrapper.cs`:
```csharp
services.AddSingleton<OllamaProvider>();
services.AddSingleton<OpenAiProvider>();
services.AddSingleton<GoogleGeminiProvider>();
services.AddSingleton<AnthropicProvider>();
services.AddSingleton<HuggingFaceRepository>();
```

---

### 3. Model Performance Evaluation System

#### Model Performance Metrics (`ModelPerformanceMetrics.cs`)
Tracks per-query metrics:
```csharp
public string ModelName { get; set; }
public string ProviderName { get; set; }
public DateTime Timestamp { get; set; }
public long ResponseTimeMs { get; set; }          // Latency
public double QualityScore { get; set; }          // 0-1 rating
public decimal CostUsd { get; set; }              // Request cost
public int? InputTokens { get; set; }
public int? OutputTokens { get; set; }
public string TaskType { get; set; }              // FileOrganization, etc.
public bool Success { get; set; }
public string? ErrorMessage { get; set; }
```

#### Model Evaluation Service (`ModelEvaluationService.cs`)
**Interface**: `IModelEvaluationService`

**Key Methods**:
- `RecordMetricsAsync()` - Log a single query's metrics
- `GetSummaryAsync()` - Get aggregated stats for one model
- `GetAllSummariesAsync()` - Get rankings of all models
- `GetMetricsAsync()` - Get detailed history
- `ClearMetricsAsync()` - Delete metrics for a model

**Storage**: Metrics saved to `%APPDATA%/Ai-Organizer/model_metrics.json`

**Example Usage**:
```csharp
var metrics = new ModelPerformanceMetrics
{
    ModelName = "llama3.2",
    ProviderName = "Ollama",
    ResponseTimeMs = 1234,
    QualityScore = 0.92,
    TaskType = "FileOrganization"
};
await evaluationService.RecordMetricsAsync(metrics, cancellationToken);
```

---

### 4. Model Browser Service (`ModelBrowser.cs`)

**Interface**: `IModelBrowser`

**Key Methods**:
- `GetAvailableModelsAsync()` - List all available models with performance data
- `GetDownloadedModelsAsync()` - List only downloaded models
- `DownloadModelAsync()` - Download with progress tracking
- `DeleteModelAsync()` - Remove model and clear metrics
- `UpdateModelAsync()` - Update to latest version
- `GetModelInfoAsync()` - Get details for one model

**Model Cache**: `%APPDATA%/Ai-Organizer/models/`
- Each provider has its own subfolder
- Marker files track download status and date

---

### 5. Interactive Configuration System

#### Organization Configuration (`OrganizationConfiguration.cs`)
Stores user preferences:
```csharp
public string OrganizationStrategy { get; set; }     // User's organization method
public double Handiness { get; set; }                // 0=automatic, 1=manual
public List<string> Constraints { get; set; }       // Rules to enforce
public List<string> StandardRules { get; set; }     // Auto-applied rules
public bool UpdateMetadata { get; set; }
public bool PreserveExistingMetadata { get; set; }
public string ExcludeFileTypes { get; set; }        // e.g., "exe,dll,sys"
public int MaxFolderDepth { get; set; }
public bool LiveUpdateDuringScan { get; set; }
```

#### Natural Language Organization Service (`NaturalLanguageOrganizationService.cs`)
**Interface**: `INaturalLanguageOrganizationService`

**Key Methods**:
- `ParseInstructionsAsync()` - Parse user input into strategy and constraints
  - Example: "Group by type then date" → strategy + constraint list
  - Recognizes patterns: size limits, date preservation, file exclusions
  
- `GenerateSystemPromptFromConfig()` - Create LLM prompt from configuration

**Pattern Recognition**:
```csharp
// Size constraints: "under 2GB per folder"
// Date preservation: "keep dates"
// Exclusions: "don't touch system files"
// Organization methods: "by type", "by date", "by project"
```

#### Interactive Config View Model (`InteractiveConfigViewModel.cs`)
**Location**: `ViewModels/InteractiveConfigViewModel.cs`

**Workflow** (4-step setup):
1. **Organization Strategy** - "How should files be organized?"
2. **Automation Level** - "How hands-on do you want to be?" (0-1 slider)
3. **Constraints** - "Any rules to enforce?"
4. **File Exclusions** - "File types to skip?"

**Properties**:
- `QuestionsAndAnswers` - Q&A history display
- `CurrentQuestion` - Current step's question
- `UserInput` - User's response
- `HandinessLevel` - Automation preference (0-1)
- `UpdateMetadata` - Enable metadata updates
- `PreserveExistingMetadata` - Keep existing metadata
- `ExcludeFileTypes` - Comma-separated exclusions
- `MaxFolderDepth` - Folder nesting limit
- `IsComplete` - Configuration finished

**Commands**:
- `SubmitAnswerCommand` - Process answer and move to next step
- `ResetCommand` - Start over

---

### 6. Enhanced Scanning with Live Updates

#### Updated ScanViewModel
**New Properties**:
```csharp
[ObservableProperty]
private int _dirsScanned;        // Real-time directory count

[ObservableProperty]
private int _filesScanned;       // Real-time file count

[ObservableProperty]
private int _filesMatched;       // Real-time matched count
```

**Live Progress**:
- Updates every time files are scanned
- Shows directory count, file count, matched count
- Provides real-time feedback during long scans

**Integration** with `InteractiveConfiguration`:
- Respects `LiveUpdateDuringScan` setting
- Can fetch organization preferences from config
- Applies constraints during scanning if configured

---

## Updated Files

### Core Models
- `Models/ModelPerformanceMetrics.cs` - NEW
- `Models/Organizing/OrganizationConfiguration.cs` - NEW
- `Models/Settings/AppSettings.cs` - UPDATED (added Gemini/Anthropic settings)

### Services - LLM
- `Services/Llm/ILLMProvider.cs` - UPDATED (new RepositoryType enum values)
- `Services/Llm/GoogleGeminiProvider.cs` - NEW
- `Services/Llm/AnthropicProvider.cs` - NEW
- `Services/Llm/ModelEvaluationService.cs` - NEW
- `Services/Llm/ModelBrowser.cs` - NEW
- `Services/Llm/ModelRepositoryService.cs` - UPDATED

### Services - Organizing
- `Services/Organizing/NaturalLanguageOrganizationService.cs` - NEW

### Services - Settings
- `Services/Settings/SecretKeys.cs` - UPDATED (added Gemini/Anthropic keys)

### ViewModels
- `ViewModels/MainWindowViewModel.cs` - UPDATED (added new tabs)
- `ViewModels/ScanViewModel.cs` - UPDATED (live progress tracking)
- `ViewModels/ModelBrowserViewModel.cs` - NEW
- `ViewModels/InteractiveConfigViewModel.cs` - NEW

### Views
- `Views/ModelBrowserView.axaml` - NEW
- `Views/ModelBrowserView.axaml.cs` - NEW
- `Views/InteractiveConfigView.axaml` - NEW
- `Views/InteractiveConfigView.axaml.cs` - NEW
- `MainWindow.axaml` - UPDATED (added tabs)

### Infrastructure
- `Infrastructure/AppBootstrapper.cs` - UPDATED (registered new services)

---

## Configuration & Secret Storage

### API Keys Storage
Keys are stored securely via `ISecretStore` (Windows Data Protection):

```csharp
// Setting a key
await secretStore.SetAsync(SecretKeys.GoogleGeminiApiKey, "sk-...", cancellationToken);

// Retrieving a key
var key = await secretStore.GetAsync(SecretKeys.GoogleGeminiApiKey, cancellationToken);
```

### AppSettings Extension
```csharp
public GoogleGeminiSettings GoogleGemini { get; set; } = new();
public AnthropicSettings Anthropic { get; set; } = new();
public bool EnablePerformanceTracking { get; set; } = true;
```

---

## Workflow Example

### Typical User Journey

1. **Start Program**
   - Opens to "Setup" tab
   - User prompted with interactive configuration wizard

2. **Configure Organization** (Interactive Config Tab)
   - Q: "How should files be organized?"
   - A: "Group by type then organize by date"
   - Q: "Automation level?"
   - A: "0.5 (ask for groups)"
   - Q: "Any constraints?"
   - A: "Max 2GB per folder, preserve dates"
   - Q: "Exclude file types?"
   - A: "exe,dll,sys"
   - Configuration saved → can proceed

3. **Browse Models** (Models Tab)
   - View all available models across providers
   - See performance rankings
   - Download/update models as needed
   - Monitor metrics for each model

4. **Scan Files** (Scan Tab)
   - Select folders/files (mixed selection)
   - Choose scan depth, glob patterns
   - Real-time progress updates
   - Live metrics on dashboard

5. **Plan & Execute** (Plan Preview Tab)
   - See proposed organization
   - Selected model used for analysis
   - Model metrics recorded
   - Execute moves/copies

6. **Review Performance**
   - Return to Models tab
   - See updated quality scores
   - Response times, costs tracked
   - Compare model effectiveness

---

## Data Storage Locations

| Data | Location |
|------|----------|
| App Settings | `%APPDATA%/Ai-Organizer/settings.json` |
| API Keys | `%APPDATA%/Ai-Organizer/secrets` (encrypted) |
| Model Metrics | `%APPDATA%/Ai-Organizer/model_metrics.json` |
| Model Cache | `%APPDATA%/Ai-Organizer/models/` |

---

## Future Enhancements

1. **Real Ollama Integration**
   - Call `ollama pull <model>` for downloads
   - Call `ollama rm <model>` for deletions
   - Get actual model sizes

2. **Advanced Constraint Validation**
   - Client-side warnings for contradictory rules
   - Server-side validation before execution
   - Constraint templates for common scenarios

3. **Model Evaluation Dashboard**
   - Historical charts of model performance
   - Cost analysis over time
   - Quality trends

4. **Batch Configuration**
   - Save/load configuration profiles
   - Quick-switch between scenarios
   - Share configurations

5. **ML-Based Preferences**
   - Learn from user corrections
   - Improve quality scores over time
   - Personalized model recommendations

---

## API Reference

### Key Interfaces

#### IModelBrowser
```csharp
Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken);
Task<bool> DownloadModelAsync(string name, string provider, 
    IProgress<ModelDownloadProgress>? progress, CancellationToken);
```

#### IModelEvaluationService
```csharp
Task RecordMetricsAsync(ModelPerformanceMetrics metrics, CancellationToken);
Task<ModelPerformanceSummary> GetSummaryAsync(string model, string provider, CancellationToken);
Task<IReadOnlyList<ModelPerformanceSummary>> GetAllSummariesAsync(CancellationToken);
```

#### INaturalLanguageOrganizationService
```csharp
Task<(string strategy, List<string> constraints)> ParseInstructionsAsync(
    string input, CancellationToken);
string GenerateSystemPromptFromConfig(OrganizationConfiguration config);
```

---

## Troubleshooting

### Models Not Loading
- Verify API keys are set in Settings
- Check internet connection
- Review application logs

### Performance Metrics Not Saved
- Enable `EnablePerformanceTracking` in AppSettings
- Check write permissions to `%APPDATA%/Ai-Organizer/`

### API Errors
- Google Gemini: Ensure model name format (models/gemini-pro)
- Anthropic: Verify API key has correct permissions
- OpenAI: Use compatible models (gpt-4, gpt-3.5-turbo)


