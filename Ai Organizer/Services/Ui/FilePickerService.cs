using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Ui;

public sealed class FilePickerService : IFilePickerService
{
    private static Window? TryGetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public async Task<IReadOnlyList<string>> PickFoldersAsync(CancellationToken cancellationToken)
    {
        var window = TryGetMainWindow();
        if (window?.StorageProvider is null)
            return new List<string>();

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select folders to scan",
            AllowMultiple = true
        });

        return folders
            .Select(f => f.TryGetLocalPath())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Cast<string>()
            .ToList();
    }

    public async Task<IReadOnlyList<string>> PickFilesAsync(CancellationToken cancellationToken)
    {
        var window = TryGetMainWindow();
        if (window?.StorageProvider is null)
            return new List<string>();

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select files to scan",
            AllowMultiple = true
        });

        return files
            .Select(f => f.TryGetLocalPath())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Cast<string>()
            .ToList();
    }
}


