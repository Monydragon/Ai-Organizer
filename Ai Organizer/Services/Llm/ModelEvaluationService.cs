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

public interface IModelEvaluationService
{
    Task RecordMetricsAsync(ModelPerformanceMetrics metrics, CancellationToken cancellationToken);
    Task<ModelPerformanceSummary?> GetSummaryAsync(string modelName, string providerName, CancellationToken cancellationToken);
    Task<IReadOnlyList<ModelPerformanceSummary>> GetAllSummariesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ModelPerformanceMetrics>> GetMetricsAsync(string modelName, string providerName, CancellationToken cancellationToken);
    Task ClearMetricsAsync(string modelName, string providerName, CancellationToken cancellationToken);
}

public sealed class ModelEvaluationService : IModelEvaluationService
{
    private readonly AppSettingsService _appSettings;
    private readonly string _metricsPath;

    public ModelEvaluationService(AppSettingsService appSettings)
    {
        _appSettings = appSettings;
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Ai-Organizer"
        );
        Directory.CreateDirectory(appDataDir);
        _metricsPath = Path.Combine(appDataDir, "model_metrics.json");
    }

    public async Task RecordMetricsAsync(ModelPerformanceMetrics metrics, CancellationToken cancellationToken)
    {
        var settings = await _appSettings.GetAsync(cancellationToken);
        if (!settings.EnablePerformanceTracking)
            return;

        var allMetrics = await LoadMetricsAsync(cancellationToken);
        allMetrics.Add(metrics);

        await SaveMetricsAsync(allMetrics, cancellationToken);
    }

    public async Task<ModelPerformanceSummary?> GetSummaryAsync(string modelName, string providerName, CancellationToken cancellationToken)
    {
        var allMetrics = await LoadMetricsAsync(cancellationToken);
        var filtered = allMetrics
            .Where(m => string.Equals(m.ModelName, modelName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(m.ProviderName, providerName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtered.Count == 0)
            return null;

        var successful = filtered.Where(m => m.Success).ToList();
        var total = filtered.Count;

        return new ModelPerformanceSummary
        {
            ModelName = modelName,
            ProviderName = providerName,
            TotalRequests = total,
            SuccessfulRequests = successful.Count,
            AverageResponseTimeMs = successful.Count > 0 ? successful.Average(m => m.ResponseTimeMs) : 0,
            AverageQualityScore = successful.Count > 0 ? successful.Average(m => m.QualityScore) : 0,
            TotalCostUsd = filtered.Sum(m => m.CostUsd),
            FirstUsed = filtered.Min(m => m.Timestamp),
            LastUsed = filtered.Max(m => m.Timestamp)
        };
    }

    public async Task<IReadOnlyList<ModelPerformanceSummary>> GetAllSummariesAsync(CancellationToken cancellationToken)
    {
        var allMetrics = await LoadMetricsAsync(cancellationToken);
        
        var grouped = allMetrics
            .GroupBy(m => (m.ModelName, m.ProviderName))
            .Select(g => new ModelPerformanceSummary
            {
                ModelName = g.Key.ModelName,
                ProviderName = g.Key.ProviderName,
                TotalRequests = g.Count(),
                SuccessfulRequests = g.Count(m => m.Success),
                AverageResponseTimeMs = g.Where(m => m.Success).Average(m => m.ResponseTimeMs),
                AverageQualityScore = g.Where(m => m.Success).Average(m => m.QualityScore),
                TotalCostUsd = g.Sum(m => m.CostUsd),
                FirstUsed = g.Min(m => m.Timestamp),
                LastUsed = g.Max(m => m.Timestamp)
            })
            .OrderByDescending(s => s.AverageQualityScore)
            .ThenBy(s => s.AverageResponseTimeMs)
            .ToList();

        return grouped;
    }

    public async Task<IReadOnlyList<ModelPerformanceMetrics>> GetMetricsAsync(string modelName, string providerName, CancellationToken cancellationToken)
    {
        var allMetrics = await LoadMetricsAsync(cancellationToken);
        return allMetrics
            .Where(m => string.Equals(m.ModelName, modelName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(m.ProviderName, providerName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Timestamp)
            .ToList();
    }

    public async Task ClearMetricsAsync(string modelName, string providerName, CancellationToken cancellationToken)
    {
        var allMetrics = await LoadMetricsAsync(cancellationToken);
        var filtered = allMetrics
            .Where(m => !(string.Equals(m.ModelName, modelName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(m.ProviderName, providerName, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        await SaveMetricsAsync(filtered, cancellationToken);
    }

    private async Task<List<ModelPerformanceMetrics>> LoadMetricsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_metricsPath))
            return new List<ModelPerformanceMetrics>();

        try
        {
            await using var stream = File.OpenRead(_metricsPath);
            var metrics = await JsonSerializer.DeserializeAsync<List<ModelPerformanceMetrics>>(stream, cancellationToken: cancellationToken);
            return metrics ?? new List<ModelPerformanceMetrics>();
        }
        catch
        {
            return new List<ModelPerformanceMetrics>();
        }
    }

    private async Task SaveMetricsAsync(List<ModelPerformanceMetrics> metrics, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        await using var stream = File.Create(_metricsPath);
        await JsonSerializer.SerializeAsync(stream, metrics, options, cancellationToken);
    }
}

