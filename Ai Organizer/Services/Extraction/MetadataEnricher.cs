using Ai_Organizer.Models.Extraction;
using Ai_Organizer.Models.Scanning;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Extraction;

public sealed class MetadataEnricher : IFileContextEnricher
{
    public bool CanHandle(FileCandidate candidate) => true;

    public Task EnrichAsync(FileCandidate candidate, FileContext context, ExtractorOptions options, CancellationToken cancellationToken)
    {
        context.SizeBytes = candidate.SizeBytes;
        context.LastWriteTimeUtc = candidate.LastWriteTime;
        context.MimeType = MimeTypes.FromExtension(Path.GetExtension(candidate.FullPath));
        return Task.CompletedTask;
    }
}


