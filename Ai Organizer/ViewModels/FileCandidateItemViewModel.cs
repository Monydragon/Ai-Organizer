using Ai_Organizer.Infrastructure;
using Ai_Organizer.Models.Scanning;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Ai_Organizer.ViewModels;

public sealed partial class FileCandidateItemViewModel : ObservableObject
{
    public FileCandidateItemViewModel(FileCandidate model)
    {
        Model = model;
    }

    public FileCandidate Model { get; }

    public string FullPath => Model.FullPath;
    public string RelativePath => Model.RelativePath;
    public string Name => Model.Name;
    public string Extension => Model.Extension;
    public long SizeBytes => Model.SizeBytes;
    public string SizeDisplay => Formatters.Bytes(Model.SizeBytes);
    public DateTimeOffset LastWriteTime => Model.LastWriteTime;
    public string LastWriteDisplay => Formatters.LocalTime(Model.LastWriteTime);

    [ObservableProperty]
    private bool _isSelected = true;
}


