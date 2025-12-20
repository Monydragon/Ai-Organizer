using Ai_Organizer.Models.Organizing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ai_Organizer.Services.Organizing;

public sealed class PlanValidator
{
    public OrganizationPlan ValidateAndNormalize(OrganizationPlan plan, IReadOnlyList<string> allowedRoots)
    {
        var normalizedRoots = allowedRoots
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => NormalizeDirOrFileRoot(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var output = new OrganizationPlan();

        foreach (var item in plan.Items)
        {
            var normalizedItem = new PlanItem
            {
                SourcePath = item.SourcePath ?? "",
                Action = item.Action,
                TargetRelativePath = item.TargetRelativePath ?? "",
                NewFileName = item.NewFileName,
                Confidence0to1 = Clamp01(item.Confidence0to1),
                Rationale = item.Rationale,
                Tags = item.Tags ?? new List<string>()
            };

            var warnings = ValidateItem(normalizedItem, normalizedRoots);
            foreach (var w in warnings)
                output.Warnings.Add(w);

            output.Items.Add(normalizedItem);
        }

        // carry forward any original warnings
        foreach (var w in plan.Warnings ?? [])
            output.Warnings.Add(w);

        return output;
    }

    private static List<string> ValidateItem(PlanItem item, IReadOnlyList<string> allowedRoots)
    {
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(item.SourcePath))
        {
            item.Action = PlanAction.Skip;
            warnings.Add("Plan item missing sourcePath; forcing Skip.");
            return warnings;
        }

        var sourceFull = SafeFullPath(item.SourcePath);
        if (sourceFull is null)
        {
            item.Action = PlanAction.Skip;
            warnings.Add($"Invalid sourcePath '{item.SourcePath}'; forcing Skip.");
            return warnings;
        }

        if (!IsWithinAllowedRoots(sourceFull, allowedRoots))
        {
            item.Action = PlanAction.Skip;
            warnings.Add($"sourcePath not within allowed roots: '{item.SourcePath}'; forcing Skip.");
            return warnings;
        }

        item.SourcePath = sourceFull;

        if (item.Action is PlanAction.Move or PlanAction.Copy)
        {
            if (!IsSafeRelativePath(item.TargetRelativePath))
            {
                item.Action = PlanAction.Skip;
                warnings.Add($"Unsafe targetRelativePath '{item.TargetRelativePath}' for '{item.SourcePath}'; forcing Skip.");
            }
        }

        if (!string.IsNullOrWhiteSpace(item.NewFileName))
        {
            var name = item.NewFileName.Trim();
            if (!IsSafeFileName(name))
            {
                warnings.Add($"Unsafe newFileName '{item.NewFileName}' for '{item.SourcePath}'; clearing rename.");
                item.NewFileName = null;
            }
            else
            {
                item.NewFileName = name;
            }
        }

        return warnings;
    }

    private static bool IsWithinAllowedRoots(string fullPath, IReadOnlyList<string> allowedRoots)
    {
        foreach (var root in allowedRoots)
        {
            // If root is a file, allow exact match.
            if (File.Exists(root) && fullPath.Equals(root, StringComparison.OrdinalIgnoreCase))
                return true;

            // Directory root
            if (Directory.Exists(root))
            {
                var rootWithSep = EnsureTrailingSeparator(root);
                if (fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private static string NormalizeDirOrFileRoot(string path)
    {
        var full = SafeFullPath(path);
        return full ?? path;
    }

    private static string? SafeFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }

    private static string EnsureTrailingSeparator(string dir)
    {
        if (dir.EndsWith(Path.DirectorySeparatorChar))
            return dir;
        return dir + Path.DirectorySeparatorChar;
    }

    private static bool IsSafeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        // Normalize separators for checks.
        var p = relativePath.Replace('\\', '/').Trim();

        if (p.StartsWith('/'))
            return false;
        if (p.Contains("..", StringComparison.Ordinal))
            return false;

        // Disallow rooted paths (Windows drive, UNC) in the original string too.
        if (Path.IsPathRooted(relativePath))
            return false;

        // Reject invalid chars
        if (p.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return false;

        return true;
    }

    private static bool IsSafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;
        if (fileName.Contains('/') || fileName.Contains('\\'))
            return false;
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;
        if (fileName is "." or "..")
            return false;
        return true;
    }

    private static double Clamp01(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v))
            return 0;
        if (v < 0) return 0;
        if (v > 1) return 1;
        return v;
    }
}


