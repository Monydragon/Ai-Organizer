using System.Threading;
using System.Threading.Tasks;
using Ai_Organizer.Models.Settings;

namespace Ai_Organizer.Services.Settings;

public interface IAppSettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}


