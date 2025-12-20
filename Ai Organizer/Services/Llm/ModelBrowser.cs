using Ai_Organizer.Models;
using Ai_Organizer.Services.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Llm;

public sealed class ModelInfo
{
    public string Name { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public RepositoryType ProviderType { get; set; }
    public bool SupportsVision { get; set; }
    public bool IsDownloaded { get; set; }
    public DateTime? DateDownloaded { get; set; }
    public long? SizeBytes { get; set; }
    public string? Description { get; set; }
    public double? PerformanceScore { get; set; } // 0-1 rating based on evaluation data
}

public interface IModelBrowser
{
    Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ModelInfo>> GetDownloadedModelsAsync(CancellationToken cancellationToken);
    Task<bool> DownloadModelAsync(string modelName, string providerName, IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken);
    Task<bool> DeleteModelAsync(string modelName, string providerName, CancellationToken cancellationToken);
    Task<bool> UpdateModelAsync(string modelName, string providerName, IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken);
    Task<ModelInfo?> GetModelInfoAsync(string modelName, string providerName, CancellationToken cancellationToken);
}

public sealed class ModelDownloadProgress
{
    public string ModelName { get; set; } = "";
    public long BytesDownloaded { get; set; }
    public long? TotalBytes { get; set; }
    public double ProgressPercent => TotalBytes.HasValue && TotalBytes > 0
        ? BytesDownloaded / (double)TotalBytes.Value * 100
        : 0;
    public string Status { get; set; } = "Downloading...";
}

public sealed class ModelBrowser : IModelBrowser
{
    private readonly IEnumerable<IModelRepository> _repositories;
    private readonly IModelEvaluationService _evaluation;
    private readonly AppSettingsService _settings;
    private readonly string _modelCachePath;

    public ModelBrowser(
        IEnumerable<IModelRepository> repositories,
        IModelEvaluationService evaluation,
        AppSettingsService settings)
    {
        _repositories = repositories;
        _evaluation = evaluation;
        _settings = settings;

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Ai-Organizer",
            "models"
        );
        Directory.CreateDirectory(appDataDir);
        _modelCachePath = appDataDir;
    }

    public async Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken)
    {
        var models = new List<ModelInfo>();

        foreach (var repo in _repositories)
        {
            try
            {
                var modelNames = await repo.ListModelsAsync(cancellationToken);
                foreach (var name in modelNames)
                {
                    var downloaded = IsModelDownloaded(name, repo.Name);
                    var size = downloaded ? GetDownloadedModelSize(name, repo.Name) : null;
                    var dateDownloaded = downloaded ? GetDownloadedModelDate(name, repo.Name) : null;
                    
                    var summary = await _evaluation.GetSummaryAsync(name, repo.Name, cancellationToken);
                    var performanceScore = summary?.AverageQualityScore;

                    models.Add(new ModelInfo
                    {
                        Name = name,
                        ProviderName = repo.Name,
                        ProviderType = repo.Type,
                        IsDownloaded = downloaded,
                        SizeBytes = size,
                        DateDownloaded = dateDownloaded,
                        PerformanceScore = performanceScore
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching models from {repo.Name}: {ex.Message}");
            }
        }

        return models.OrderBy(m => m.ProviderName).ThenBy(m => m.Name).ToList();
    }

    public async Task<IReadOnlyList<ModelInfo>> GetDownloadedModelsAsync(CancellationToken cancellationToken)
    {
        var all = await GetAvailableModelsAsync(cancellationToken);
        return all.Where(m => m.IsDownloaded).OrderBy(m => m.DateDownloaded).ToList();
    }

    public async Task<bool> DownloadModelAsync(string modelName, string providerName, IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken)
    {
        try
        {
            progress?.Report(new ModelDownloadProgress { ModelName = modelName, Status = $"Downloading {modelName} from {providerName}..." });

            // For Ollama, models are managed by Ollama itself (pull command)
            // For API-based providers, we just mark as "available" since they're downloaded on-demand
            if (providerName.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                // In a real implementation, you'd call ollama pull here
                // For now, we just create a marker file
                var markerPath = GetModelMarkerPath(modelName, providerName);
                Directory.CreateDirectory(Path.GetDirectoryName(markerPath) ?? "");
                File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
                progress?.Report(new ModelDownloadProgress { ModelName = modelName, Status = "Download complete", BytesDownloaded = 1, TotalBytes = 1 });
                return true;
            }

            // For API providers, just mark as downloaded
            var apiMarkerPath = GetModelMarkerPath(modelName, providerName);
            Directory.CreateDirectory(Path.GetDirectoryName(apiMarkerPath) ?? "");
            File.WriteAllText(apiMarkerPath, DateTime.UtcNow.ToString("O"));
            progress?.Report(new ModelDownloadProgress { ModelName = modelName, Status = "Model registered", BytesDownloaded = 1, TotalBytes = 1 });
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Download failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteModelAsync(string modelName, string providerName, CancellationToken cancellationToken)
    {
        try
        {
            var markerPath = GetModelMarkerPath(modelName, providerName);
            if (File.Exists(markerPath))
                File.Delete(markerPath);

            // For Ollama, you'd call ollama rm here
            // For API providers, just remove the marker

            await _evaluation.ClearMetricsAsync(modelName, providerName, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateModelAsync(string modelName, string providerName, IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken)
    {
        try
        {
            progress?.Report(new ModelDownloadProgress { ModelName = modelName, Status = $"Updating {modelName}..." });

            // For Ollama, call ollama pull to get latest
            // For API providers, models are always up-to-date
            if (providerName.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                var markerPath = GetModelMarkerPath(modelName, providerName);
                Directory.CreateDirectory(Path.GetDirectoryName(markerPath) ?? "");
                File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
                progress?.Report(new ModelDownloadProgress { ModelName = modelName, Status = "Update complete", BytesDownloaded = 1, TotalBytes = 1 });
                return true;
            }

            progress?.Report(new ModelDownloadProgress { ModelName = modelName, Status = "Already up-to-date", BytesDownloaded = 1, TotalBytes = 1 });
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update failed: {ex.Message}");
            return false;
        }
    }

    public async Task<ModelInfo?> GetModelInfoAsync(string modelName, string providerName, CancellationToken cancellationToken)
    {
        var all = await GetAvailableModelsAsync(cancellationToken);
        return all.FirstOrDefault(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase)
            && m.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsModelDownloaded(string modelName, string providerName)
    {
        var markerPath = GetModelMarkerPath(modelName, providerName);
        return File.Exists(markerPath);
    }

    private string GetModelMarkerPath(string modelName, string providerName)
    {
        var safeName = string.Join("_", modelName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_modelCachePath, providerName.ToLowerInvariant(), $"{safeName}.downloaded");
    }

    private long? GetDownloadedModelSize(string modelName, string providerName)
    {
        // In a real implementation, get actual size from Ollama or storage
        return null;
    }

    private DateTime? GetDownloadedModelDate(string modelName, string providerName)
    {
        var markerPath = GetModelMarkerPath(modelName, providerName);
        if (File.Exists(markerPath))
        {
            try
            {
                var content = File.ReadAllText(markerPath);
                if (DateTime.TryParse(content, out var date))
                    return date;
            }
            catch { }
        }
        return null;
    }
}

