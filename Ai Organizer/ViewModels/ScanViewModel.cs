using Ai_Organizer.Models.Scanning;
using Ai_Organizer.Services.Scanning;
using Ai_Organizer.Services.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.ViewModels;

public sealed partial class ScanViewModel : ObservableObject
{
    private readonly IFilePickerService _filePicker;
    private readonly FileScanner _scanner;
    private CancellationTokenSource? _scanCts;

    public ScanViewModel(IFilePickerService filePicker, FileScanner scanner)
    {
        _filePicker = filePicker;
        _scanner = scanner;
        Roots.CollectionChanged += (_, _) => UpdateRootsSummary();
        UpdateRootsSummary();
    }

    public ObservableCollection<string> Roots { get; } = new();
    public ObservableCollection<FileCandidateItemViewModel> Candidates { get; } = new();

    [ObservableProperty]
    private string _selectedRootsSummary = "No roots selected yet.";

    [ObservableProperty]
    private bool _includeHidden = false;

    [ObservableProperty]
    private int _maxDepth = 12;

    [ObservableProperty]
    private string _includeGlob = "**/*";

    [ObservableProperty]
    private string _excludeGlob = "**/bin/**;**/obj/**";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _scanStatus = "Idle.";

    [RelayCommand]
    private async Task AddFoldersAsync()
    {
        var folders = await _filePicker.PickFoldersAsync(CancellationToken.None);
        foreach (var f in folders)
        {
            if (!Roots.Contains(f))
                Roots.Add(f);
        }
        UpdateRootsSummary();
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var files = await _filePicker.PickFilesAsync(CancellationToken.None);
        foreach (var f in files)
        {
            if (!Roots.Contains(f))
                Roots.Add(f);
        }
        UpdateRootsSummary();
    }

    [RelayCommand]
    private void ClearRoots()
    {
        Roots.Clear();
        UpdateRootsSummary();
    }

    [RelayCommand]
    private void RemoveRoot(string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
            return;
        Roots.Remove(root);
        UpdateRootsSummary();
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        if (!CanScan())
            return;

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        IsScanning = true;
        ScanStatus = "Scanning...";
        Candidates.Clear();

        var options = new ScanOptions
        {
            Roots = Roots.ToList(),
            IncludeHidden = IncludeHidden,
            MaxDepth = MaxDepth,
            IncludeGlob = IncludeGlob,
            ExcludeGlob = ExcludeGlob
        };

        var progress = new Progress<ScanProgress>(p =>
        {
            ScanStatus = $"Root: {p.CurrentRoot} | Dirs: {p.DirectoriesVisited:N0} | Files: {p.FilesVisited:N0} | Matched: {p.FilesMatched:N0}";
        });

        try
        {
            var results = await _scanner.ScanAsync(options, progress, ct);
            foreach (var r in results)
                Candidates.Add(new FileCandidateItemViewModel(r));

            ScanStatus = $"Done. Matched {Candidates.Count:N0} files.";
        }
        catch (OperationCanceledException)
        {
            ScanStatus = $"Canceled. Matched {Candidates.Count:N0} files so far.";
        }
        finally
        {
            IsScanning = false;
            ScanCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        _scanCts?.Cancel();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var c in Candidates)
            c.IsSelected = true;
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var c in Candidates)
            c.IsSelected = false;
    }

    private bool CanScan() => Roots.Count > 0 && !IsScanning;

    private void UpdateRootsSummary()
    {
        if (Roots.Count == 0)
        {
            SelectedRootsSummary = "No roots selected yet.";
            return;
        }

        SelectedRootsSummary = $"{Roots.Count} root(s) selected.";
    }
}


