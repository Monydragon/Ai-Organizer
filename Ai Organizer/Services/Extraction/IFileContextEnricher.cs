using Ai_Organizer.Models.Extraction;
using Ai_Organizer.Models.Scanning;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Extraction;

public interface IFileContextEnricher
{
    bool CanHandle(FileCandidate candidate);
    Task EnrichAsync(FileCandidate candidate, FileContext context, ExtractorOptions options, CancellationToken cancellationToken);
}


