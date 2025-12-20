using System.Collections.Generic;
using Ai_Organizer.ViewModels;

namespace Ai_Organizer.Infrastructure;

public static class ModelSortByValuesProvider
{
    public static IReadOnlyList<ModelSortBy> Values { get; } = new[]
    {
        ModelSortBy.Name,
        ModelSortBy.Provider,
        ModelSortBy.Size,
        ModelSortBy.Downloaded,
        ModelSortBy.DownloadedDate,
        ModelSortBy.Vision,
        ModelSortBy.LastUsed,
        ModelSortBy.Score
    };
}

