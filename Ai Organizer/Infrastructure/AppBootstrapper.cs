using Ai_Organizer.Services.Settings;
using Ai_Organizer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Ai_Organizer.Infrastructure;

public static class AppBootstrapper
{
    public static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        // Settings + persistence
        services.AddSingleton<IAppSettingsStore, JsonAppSettingsStore>();
        services.AddSingleton<AppSettingsService>();

        // View-models
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ScanViewModel>();
        services.AddSingleton<PlanPreviewViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Views / windows
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}


