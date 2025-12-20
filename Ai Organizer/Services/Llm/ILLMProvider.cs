using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Llm;

public interface ILLMProvider
{
    string Name { get; }
    bool SupportsVision { get; }

    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Executes a chat request and returns the assistant's JSON string (not parsed).
    /// </summary>
    Task<string> ChatJsonAsync(ChatJsonRequest request, CancellationToken cancellationToken);
}


