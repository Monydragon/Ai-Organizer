using Ai_Organizer.Models.Scanning;
using Ai_Organizer.Services.Scanning;
using Ai_Organizer.Services.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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
    public ObservableCollection<FileCandidateItemViewModel> FilteredCandidates { get; } = new();

    [ObservableProperty]
    private string _selectedRootsSummary = "No roots selected yet.";

    [ObservableProperty]
    private bool _includeHidden;

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

    [ObservableProperty]
    private int _dirsScanned;

    [ObservableProperty]
    private int _filesScanned;

    [ObservableProperty]
    private int _filesMatched;

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

    [ObservableProperty]
    private FileCandidateItemViewModel? _selectedCandidate;

    [ObservableProperty]
    private string _candidateSearchText = "";

    partial void OnCandidateSearchTextChanged(string value)
    {
        _ = value;
        ApplyCandidateFilter();
    }

    public int SelectedCount => Candidates.Count(c => c.IsSelected);
    public long SelectedBytes => Candidates.Where(c => c.IsSelected).Sum(c => c.SizeBytes);
    public string SelectedSizeDisplay => Infrastructure.Formatters.Bytes(SelectedBytes);

    partial void OnSelectedCandidateChanged(FileCandidateItemViewModel? value)
    {
        _ = value;
        // no-op for now; exists to enable future preview loading hooks
    }

    private void WireCandidate(FileCandidateItemViewModel item)
    {
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FileCandidateItemViewModel.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(SelectedBytes));
                OnPropertyChanged(nameof(SelectedSizeDisplay));
            }
        };
    }

    private void ApplyCandidateFilter()
    {
        var q = CandidateSearchText.Trim();

        IEnumerable<FileCandidateItemViewModel> query = Candidates;
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c =>
                c.RelativePath.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.Extension.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        FilteredCandidates.Clear();
        foreach (var c in query)
            FilteredCandidates.Add(c);
    }

    [RelayCommand]
    private void SelectInvert()
    {
        foreach (var c in Candidates)
            c.IsSelected = !c.IsSelected;
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        var toRemove = Candidates.Where(c => c.IsSelected).ToList();
        foreach (var c in toRemove)
            Candidates.Remove(c);

        ApplyCandidateFilter();
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedBytes));
        OnPropertyChanged(nameof(SelectedSizeDisplay));
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
        FilteredCandidates.Clear();

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
            DirsScanned = p.DirectoriesVisited;
            FilesScanned = p.FilesVisited;
            FilesMatched = p.FilesMatched;
            ScanStatus = $"Root: {p.CurrentRoot} | Dirs: {p.DirectoriesVisited:N0} | Files: {p.FilesVisited:N0} | Matched: {p.FilesMatched:N0}";
        });

        try
        {
            var results = await _scanner.ScanAsync(options, progress, ct);
            foreach (var r in results)
            {
                var vm = new FileCandidateItemViewModel(r);
                WireCandidate(vm);
                Candidates.Add(vm);
            }

            ApplyCandidateFilter();
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedBytes));
            OnPropertyChanged(nameof(SelectedSizeDisplay));

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

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedBytes));
        OnPropertyChanged(nameof(SelectedSizeDisplay));
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var c in Candidates)
            c.IsSelected = false;

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedBytes));
        OnPropertyChanged(nameof(SelectedSizeDisplay));
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

