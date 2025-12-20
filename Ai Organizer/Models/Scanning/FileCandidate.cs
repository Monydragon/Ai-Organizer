using System;
using System.IO;

namespace Ai_Organizer.Models.Scanning;

public sealed class FileCandidate
{
    public required string RootPath { get; init; }
    public required string FullPath { get; init; }
    public required string RelativePath { get; init; }

    public required string Name { get; init; }
    public required string Extension { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTimeOffset LastWriteTime { get; init; }
    public required FileAttributes Attributes { get; init; }
}


