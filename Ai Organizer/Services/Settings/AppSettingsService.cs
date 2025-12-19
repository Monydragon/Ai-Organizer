using Ai_Organizer.Models.Settings;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Settings;

public sealed class AppSettingsService
{
    private readonly IAppSettingsStore _store;
    private AppSettings? _cached;

    public AppSettingsService(IAppSettingsStore store)
    {
        _store = store;
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is not null)
            return _cached;

        _cached = await _store.LoadAsync(cancellationToken);
        return _cached;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        _cached = settings;
        await _store.SaveAsync(settings, cancellationToken);
    }

    public void InvalidateCache() => _cached = null;
}


