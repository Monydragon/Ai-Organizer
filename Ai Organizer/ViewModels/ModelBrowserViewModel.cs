using Ai_Organizer.Models;
using Ai_Organizer.Services.Llm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.ViewModels;

public enum ModelSortBy
{
    Name,
    Provider,
    Size,
    Downloaded,
    DownloadedDate,
    Vision,
    LastUsed,
    Score
}

public sealed partial class ModelBrowserViewModel : ObservableObject
{
    private readonly IModelBrowser _modelBrowser;
    private readonly IModelEvaluationService _evaluation;
    private CancellationTokenSource? _cts;

    public ModelBrowserViewModel(IModelBrowser modelBrowser, IModelEvaluationService evaluation)
    {
        _modelBrowser = modelBrowser;
        _evaluation = evaluation;

        // Defaults
        this._selectedProvider = "All";
        this._sortBy = ModelSortBy.Provider;
        this._sortDescending = false;
    }

    public ObservableCollection<ModelInfo> AvailableModels { get; } = new();
    public ObservableCollection<ModelInfo> DownloadedModels { get; } = new();
    public ObservableCollection<ModelPerformanceSummary> ModelPerformance { get; } = new();

    // UI-facing catalog (filtered/sorted)
    public ObservableCollection<ModelCatalogItemViewModel> FilteredModels { get; } = new();
    public ObservableCollection<string> ProviderOptions { get; } = new() { "All" };

    [ObservableProperty]
    private ModelCatalogItemViewModel? _selectedModelItem;

    public ModelInfo? SelectedModelInfo => SelectedModelItem?.Model;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedProvider;

    [ObservableProperty]
    private bool _showDownloadedOnly;

    [ObservableProperty]
    private bool _showVisionOnly;

    [ObservableProperty]
    private ModelSortBy _sortBy;

    [ObservableProperty]
    private bool _sortDescending;

    partial void OnSearchTextChanged(string value) { _ = value; ApplyFiltersAndSort(); }
    partial void OnSelectedProviderChanged(string value) { _ = value; ApplyFiltersAndSort(); }
    partial void OnShowDownloadedOnlyChanged(bool value) { _ = value; ApplyFiltersAndSort(); }
    partial void OnShowVisionOnlyChanged(bool value) { _ = value; ApplyFiltersAndSort(); }
    partial void OnSortByChanged(ModelSortBy value) { _ = value; ApplyFiltersAndSort(); }
    partial void OnSortDescendingChanged(bool value) { _ = value; ApplyFiltersAndSort(); }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _downloadProgress;

    [RelayCommand]
    public async Task LoadModelsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading models...";
        _cts = new CancellationTokenSource();

        try
        {
            var models = await _modelBrowser.GetAvailableModelsAsync(_cts.Token);

            AvailableModels.Clear();
            foreach (var model in models)
                AvailableModels.Add(model);

            var downloaded = await _modelBrowser.GetDownloadedModelsAsync(_cts.Token);
            DownloadedModels.Clear();
            foreach (var model in downloaded)
                DownloadedModels.Add(model);

            var summaries = await _evaluation.GetAllSummariesAsync(_cts.Token);
            ModelPerformance.Clear();
            foreach (var summary in summaries)
                ModelPerformance.Add(summary);

            RefreshProviderOptions(models);
            ApplyFiltersAndSort();

            StatusMessage = $"Loaded {models.Count} models";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            _cts?.Dispose();
        }
    }

    private void RefreshProviderOptions(IReadOnlyList<ModelInfo> models)
    {
        var providers = models
            .Select(m => m.ProviderName)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        ProviderOptions.Clear();
        ProviderOptions.Add("All");
        foreach (var p in providers)
            ProviderOptions.Add(p);

        if (!ProviderOptions.Contains(SelectedProvider))
            SelectedProvider = "All";
    }

    private void ApplyFiltersAndSort()
    {
        IEnumerable<ModelInfo> query = AvailableModels;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            query = query.Where(m =>
                (!string.IsNullOrWhiteSpace(m.Name) && m.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(m.ProviderName) && m.ProviderName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(m.Description) && m.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
            );
        }

        if (!string.IsNullOrWhiteSpace(SelectedProvider) && !SelectedProvider.Equals("All", StringComparison.OrdinalIgnoreCase))
            query = query.Where(m => string.Equals(m.ProviderName, SelectedProvider, StringComparison.OrdinalIgnoreCase));

        if (ShowDownloadedOnly)
            query = query.Where(m => m.IsDownloaded);

        if (ShowVisionOnly)
            query = query.Where(m => m.SupportsVision);

        // Precompute per-model last-used from performance summaries
        var lastUsedByKey = ModelPerformance
            .GroupBy(s => (s.ProviderName, s.ModelName), StringTupleComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Max(x => x.LastUsed), StringTupleComparer.OrdinalIgnoreCase);

        query = SortBy switch
        {
            ModelSortBy.Name => SortDescending ? query.OrderByDescending(m => m.Name) : query.OrderBy(m => m.Name),
            ModelSortBy.Provider => SortDescending ? query.OrderByDescending(m => m.ProviderName).ThenBy(m => m.Name) : query.OrderBy(m => m.ProviderName).ThenBy(m => m.Name),
            ModelSortBy.Size => SortDescending ? query.OrderByDescending(m => m.SizeBytes ?? -1) : query.OrderBy(m => m.SizeBytes ?? long.MaxValue),
            ModelSortBy.Downloaded => SortDescending ? query.OrderByDescending(m => m.IsDownloaded) : query.OrderBy(m => m.IsDownloaded),
            ModelSortBy.DownloadedDate => SortDescending
                ? query.OrderByDescending(m => m.DateDownloaded ?? DateTime.MinValue)
                : query.OrderBy(m => m.DateDownloaded ?? DateTime.MaxValue),
            ModelSortBy.Vision => SortDescending ? query.OrderByDescending(m => m.SupportsVision) : query.OrderBy(m => m.SupportsVision),
            ModelSortBy.LastUsed => SortDescending
                ? query.OrderByDescending(m => lastUsedByKey.GetValueOrDefault((m.ProviderName, m.Name), DateTime.MinValue))
                : query.OrderBy(m => lastUsedByKey.GetValueOrDefault((m.ProviderName, m.Name), DateTime.MaxValue)),
            ModelSortBy.Score => SortDescending ? query.OrderByDescending(m => m.PerformanceScore ?? -1) : query.OrderBy(m => m.PerformanceScore ?? double.MaxValue),
            _ => query
        };

        // Preserve selection if possible
        (string ProviderName, string Name)? selectedKey = SelectedModelItem is null
            ? null
            : (SelectedModelItem.ProviderName, SelectedModelItem.Name);

        FilteredModels.Clear();
        foreach (var m in query)
            FilteredModels.Add(new ModelCatalogItemViewModel(m));

        if (selectedKey.HasValue)
        {
            SelectedModelItem = FilteredModels.FirstOrDefault(x =>
                string.Equals(x.ProviderName, selectedKey.Value.ProviderName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Name, selectedKey.Value.Name, StringComparison.OrdinalIgnoreCase));
        }
    }

    [RelayCommand]
    public async Task DownloadModelAsync(ModelInfo? model)
    {
        if (model == null)
            return;

        IsLoading = true;
        StatusMessage = $"Downloading {model.Name}...";
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<ModelDownloadProgress>(p =>
            {
                DownloadProgress = p.ProgressPercent;
                StatusMessage = p.Status;
            });

            var success = await _modelBrowser.DownloadModelAsync(
                model.Name,
                model.ProviderName,
                progress,
                _cts.Token
            );

            if (success)
            {
                StatusMessage = $"Downloaded {model.Name}";
                await LoadModelsAsync();
            }
            else
            {
                StatusMessage = $"Failed to download {model.Name}";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Download cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            DownloadProgress = 0;
            _cts?.Dispose();
        }
    }

    [RelayCommand]
    public async Task DeleteModelAsync(ModelInfo? model)
    {
        if (model == null)
            return;

        IsLoading = true;
        StatusMessage = $"Deleting {model.Name}...";
        _cts = new CancellationTokenSource();

        try
        {
            var success = await _modelBrowser.DeleteModelAsync(
                model.Name,
                model.ProviderName,
                _cts.Token
            );

            if (success)
            {
                StatusMessage = $"Deleted {model.Name}";
                await LoadModelsAsync();
            }
            else
            {
                StatusMessage = $"Failed to delete {model.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            _cts?.Dispose();
        }
    }

    [RelayCommand]
    public async Task UpdateModelAsync(ModelInfo? model)
    {
        if (model == null)
            return;

        IsLoading = true;
        StatusMessage = $"Updating {model.Name}...";
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<ModelDownloadProgress>(p =>
            {
                DownloadProgress = p.ProgressPercent;
                StatusMessage = p.Status;
            });

            var success = await _modelBrowser.UpdateModelAsync(
                model.Name,
                model.ProviderName,
                progress,
                _cts.Token
            );

            if (success)
            {
                StatusMessage = $"Updated {model.Name}";
                await LoadModelsAsync();
            }
            else
            {
                StatusMessage = $"Failed to update {model.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            DownloadProgress = 0;
            _cts?.Dispose();
        }
    }

    [RelayCommand]
    public void CancelOperation()
    {
        _cts?.Cancel();
    }

    private static class StringTupleComparer
    {
        public static IEqualityComparer<(string ProviderName, string ModelName)> OrdinalIgnoreCase { get; } = new TupleIgnoreCaseComparer();

        private sealed class TupleIgnoreCaseComparer : IEqualityComparer<(string ProviderName, string ModelName)>
        {
            public bool Equals((string ProviderName, string ModelName) x, (string ProviderName, string ModelName) y)
                => string.Equals(x.ProviderName, y.ProviderName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(x.ModelName, y.ModelName, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string ProviderName, string ModelName) obj)
            {
                var h1 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ProviderName ?? "");
                var h2 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ModelName ?? "");
                return HashCode.Combine(h1, h2);
            }
        }
    }
}
