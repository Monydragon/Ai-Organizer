using Ai_Organizer.Models.Extraction;
using Ai_Organizer.Models.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Extraction;

public sealed class FileContextBuilder
{
    private readonly IReadOnlyList<IFileContextEnricher> _enrichers;

    public FileContextBuilder(IEnumerable<IFileContextEnricher> enrichers)
    {
        _enrichers = enrichers.ToList();
    }

    public async Task<FileContext> BuildAsync(FileCandidate candidate, ExtractorOptions options, CancellationToken cancellationToken)
    {
        var ctx = new FileContext
        {
            SourcePath = candidate.FullPath,
            FileName = Path.GetFileName(candidate.FullPath),
            Extension = Path.GetExtension(candidate.FullPath).TrimStart('.').ToLowerInvariant()
        };

        foreach (var enricher in _enrichers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!enricher.CanHandle(candidate))
                continue;

            await enricher.EnrichAsync(candidate, ctx, options, cancellationToken);
        }

        return ctx;
    }

    public async Task<IReadOnlyList<FileContext>> BuildManyAsync(
        IReadOnlyList<FileCandidate> candidates,
        ExtractorOptions options,
        IProgress<(int done, int total)>? progress,
        CancellationToken cancellationToken)
    {
        var result = new List<FileContext>(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ctx = await BuildAsync(candidates[i], options, cancellationToken);
            result.Add(ctx);
            progress?.Report((i + 1, candidates.Count));
        }
        return result;
    }
}


