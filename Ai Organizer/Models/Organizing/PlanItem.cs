using System.Collections.Generic;

namespace Ai_Organizer.Models.Organizing;

public sealed class PlanItem
{
    public string SourcePath { get; set; } = "";
    public PlanAction Action { get; set; } = PlanAction.Skip;

    /// <summary>
    /// Destination path, relative to the chosen destination root folder.
    /// Example: "Photos/2025/Trip/".
    /// </summary>
    public string TargetRelativePath { get; set; } = "";

    /// <summary>
    /// Optional new filename (no directories). If null/empty, keep original.
    /// </summary>
    public string? NewFileName { get; set; }

    public double Confidence0to1 { get; set; }
    public string? Rationale { get; set; }
    public List<string> Tags { get; set; } = new();
}


