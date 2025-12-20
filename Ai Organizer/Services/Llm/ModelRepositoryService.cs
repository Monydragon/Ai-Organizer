using Ai_Organizer.Models.Settings;
using Ai_Organizer.Services.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Llm;

public sealed class ModelRepositoryService
{
    private readonly AppSettingsService _settings;
    private readonly IEnumerable<IModelRepository> _repositories;

    public ModelRepositoryService(AppSettingsService settings, IEnumerable<IModelRepository> repositories)
    {
        _settings = settings;
        _repositories = repositories;
    }

    public async Task<IReadOnlyList<IModelRepository>> GetEnabledAsync(CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        return _repositories.Where(r => r.Enabled && IsEnabled(r.Type, settings)).ToList();
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(string repositoryName, CancellationToken cancellationToken)
    {
        var repo = (await GetEnabledAsync(cancellationToken)).FirstOrDefault(r => string.Equals(r.Name, repositoryName, StringComparison.OrdinalIgnoreCase));
        if (repo is null)
            return Array.Empty<string>();
        return await repo.ListModelsAsync(cancellationToken);
    }

    private static bool IsEnabled(RepositoryType type, AppSettings settings)
    {
        return type switch
        {
            RepositoryType.Ollama => true,
            RepositoryType.OpenAi => true,
            RepositoryType.HuggingFace => settings.HuggingFace.Enabled,
            RepositoryType.GoogleGemini => settings.GoogleGemini.HasApiKey,
            RepositoryType.Anthropic => settings.Anthropic.HasApiKey,
            _ => false
        };
    }
}

