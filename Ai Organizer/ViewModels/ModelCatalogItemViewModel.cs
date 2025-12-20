using Ai_Organizer.Infrastructure;
using Ai_Organizer.Services.Llm;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ai_Organizer.ViewModels;

public sealed class ModelCatalogItemViewModel : ObservableObject
{
    public ModelCatalogItemViewModel(ModelInfo model)
    {
        Model = model;
    }

    public ModelInfo Model { get; }

    public string Name => Model.Name;
    public string ProviderName => Model.ProviderName;
    public RepositoryType ProviderType => Model.ProviderType;

    public bool IsDownloaded => Model.IsDownloaded;
    public bool SupportsVision => Model.SupportsVision;

    public long? SizeBytes => Model.SizeBytes;
    public string SizeDisplay => Model.SizeBytes.HasValue ? Formatters.Bytes(Model.SizeBytes.Value) : "—";

    public double? PerformanceScore => Model.PerformanceScore;
    public string ScoreDisplay => Model.PerformanceScore.HasValue ? $"{Model.PerformanceScore.Value:P0}" : "—";

    public string? Description => Model.Description;
    public string DescriptionDisplay => string.IsNullOrWhiteSpace(Model.Description) ? "" : Model.Description!;

    public string DateDownloadedDisplay => Model.DateDownloaded.HasValue ? Model.DateDownloaded.Value.ToLocalTime().ToString("yyyy-MM-dd") : "—";
}
