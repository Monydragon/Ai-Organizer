using Ai_Organizer.Models;
using Ai_Organizer.Services.Llm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.ViewModels;

public sealed partial class ModelBrowserViewModel : ObservableObject
{
    private readonly IModelBrowser _modelBrowser;
    private readonly IModelEvaluationService _evaluation;
    private CancellationTokenSource? _cts;

    public ModelBrowserViewModel(IModelBrowser modelBrowser, IModelEvaluationService evaluation)
    {
        _modelBrowser = modelBrowser;
        _evaluation = evaluation;
    }

    public ObservableCollection<ModelInfo> AvailableModels { get; } = new();
    public ObservableCollection<ModelInfo> DownloadedModels { get; } = new();
    public ObservableCollection<ModelPerformanceSummary> ModelPerformance { get; } = new();

    [ObservableProperty]
    private ModelInfo? _selectedModel;

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

    [RelayCommand]
    public async Task DownloadModelAsync(ModelInfo model)
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
    public async Task DeleteModelAsync(ModelInfo model)
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
    public async Task UpdateModelAsync(ModelInfo model)
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
}

