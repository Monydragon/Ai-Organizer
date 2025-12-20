using System;
using System.Collections.Generic;

namespace Ai_Organizer.Models.Organizing;

/// <summary>
/// Represents organization constraints and preferences configured through interactive setup.
/// </summary>
public sealed class OrganizationConfiguration
{
    /// <summary>
    /// User's natural language description of how files should be organized.
    /// </summary>
    public string OrganizationStrategy { get; set; } = "";

    /// <summary>
    /// Handiness level: 0 = fully automatic, 1 = ask for confirmation on each, 0.5 = ask for groups
    /// </summary>
    public double Handiness { get; set; } = 0.5;

    /// <summary>
    /// Constraints that should be enforced (e.g., "max 2GB per folder", "preserve dates").
    /// </summary>
    public List<string> Constraints { get; set; } = new();

    /// <summary>
    /// Standard rules that apply automatically without asking.
    /// </summary>
    public List<string> StandardRules { get; set; } = new();

    /// <summary>
    /// Whether to update file metadata (dates, names) based on content analysis.
    /// </summary>
    public bool UpdateMetadata { get; set; } = true;

    /// <summary>
    /// Whether to preserve existing file metadata if found.
    /// </summary>
    public bool PreserveExistingMetadata { get; set; } = true;

    /// <summary>
    /// Comma-separated list of file types to avoid organizing (e.g., "exe,dll,sys").
    /// </summary>
    public string ExcludeFileTypes { get; set; } = "";

    /// <summary>
    /// Maximum folder depth for organization.
    /// </summary>
    public int MaxFolderDepth { get; set; } = 5;

    /// <summary>
    /// Whether to perform live updates during scanning.
    /// </summary>
    public bool LiveUpdateDuringScan { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

