using Ai_Organizer.Models.Organizing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Organizing;

public interface INaturalLanguageOrganizationService
{
    /// <summary>
    /// Parse natural language instructions into organization strategy and constraints.
    /// </summary>
    Task<(string strategy, List<string> constraints)> ParseInstructionsAsync(
        string userInput,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generate a system prompt based on the organization configuration.
    /// </summary>
    string GenerateSystemPromptFromConfig(OrganizationConfiguration config);
}

public sealed class NaturalLanguageOrganizationService : INaturalLanguageOrganizationService
{
    public Task<(string strategy, List<string> constraints)> ParseInstructionsAsync(
        string userInput,
        CancellationToken cancellationToken)
    {
        // Simple rule-based parser for common organization patterns
        var strategy = userInput;
        var constraints = new List<string>();

        // Extract constraints from common patterns
        var lower = userInput.ToLowerInvariant();

        // Size constraints
        if (lower.Contains("under") || lower.Contains("max") || lower.Contains("less than"))
        {
            var sizePattern = System.Text.RegularExpressions.Regex.Match(lower, @"(\d+\s*(?:gb|mb|tb))", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (sizePattern.Success)
                constraints.Add($"MaxFolderSize: {sizePattern.Value}");
        }

        // Date preservation
        if (lower.Contains("keep") && lower.Contains("date"))
            constraints.Add("PreserveDates");

        // File type exclusions
        if (lower.Contains("don't touch") || lower.Contains("exclude") || lower.Contains("skip"))
        {
            constraints.Add("SkipSystemFiles");
        }

        // Organization methods
        if (lower.Contains("by type") || lower.Contains("by extension"))
            strategy = "Organize by file type/extension";
        else if (lower.Contains("by date") || lower.Contains("by created") || lower.Contains("by modified"))
            strategy = "Organize by date (year/month)";
        else if (lower.Contains("by name") || lower.Contains("alphabetical"))
            strategy = "Organize alphabetically by name";
        else if (lower.Contains("by project") || lower.Contains("by folder"))
            strategy = "Organize by project/folder name";

        return Task.FromResult((strategy, constraints));
    }

    public string GenerateSystemPromptFromConfig(OrganizationConfiguration config)
    {
        var lines = new List<string>
        {
            "You are a file organization assistant.",
            "Return ONLY valid JSON matching the schema.",
            "",
            "Organization Strategy:",
            config.OrganizationStrategy,
            ""
        };

        if (config.Constraints.Any())
        {
            lines.Add("Constraints to enforce:");
            foreach (var constraint in config.Constraints)
                lines.Add($"- {constraint}");
            lines.Add("");
        }

        if (config.StandardRules.Any())
        {
            lines.Add("Standard rules (apply automatically):");
            foreach (var rule in config.StandardRules)
                lines.Add($"- {rule}");
            lines.Add("");
        }

        lines.Add($"Update Metadata: {config.UpdateMetadata}");
        lines.Add($"Preserve Existing Metadata: {config.PreserveExistingMetadata}");
        lines.Add($"Max Folder Depth: {config.MaxFolderDepth}");

        if (!string.IsNullOrWhiteSpace(config.ExcludeFileTypes))
        {
            lines.Add($"Exclude file types: {config.ExcludeFileTypes}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

