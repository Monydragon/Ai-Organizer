using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Settings;

public interface ISecretStore
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken);
    Task SetAsync(string key, string? value, CancellationToken cancellationToken);
}


