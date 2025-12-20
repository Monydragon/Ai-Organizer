using System.Collections.Generic;

namespace Ai_Organizer.Models.Scanning;

public sealed class ScanOptions
{
    public required IReadOnlyList<string> Roots { get; init; }
    public int MaxDepth { get; init; } = 12;
    public bool IncludeHidden { get; init; }
    public string IncludeGlob { get; init; } = "**/*";
    public string ExcludeGlob { get; init; } = "**/bin/**;**/obj/**";

    public long? MinSizeBytes { get; init; }
    public long? MaxSizeBytes { get; init; }
}


