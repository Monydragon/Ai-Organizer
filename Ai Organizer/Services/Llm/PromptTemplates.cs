using Ai_Organizer.Models.Extraction;
using System.Collections.Generic;
using System.Text;

namespace Ai_Organizer.Services.Llm;

public static class PromptTemplates
{
    public static string BuildPlannerSystemPrompt(string? userDefaultPrompt)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(userDefaultPrompt))
        {
            sb.AppendLine(userDefaultPrompt.Trim());
            sb.AppendLine();
        }

        sb.AppendLine("You are an assistant that organizes files into folders.");
        sb.AppendLine("Return ONLY valid JSON. No markdown. No extra keys.");
        sb.AppendLine();
        sb.AppendLine("JSON schema (informal):");
        sb.AppendLine("{");
        sb.AppendLine("  \"items\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"sourcePath\": \"string\",");
        sb.AppendLine("      \"action\": \"Move|Copy|Skip\",");
        sb.AppendLine("      \"targetRelativePath\": \"string\",");
        sb.AppendLine("      \"newFileName\": \"string|null\",");
        sb.AppendLine("      \"confidence0to1\": 0.0,");
        sb.AppendLine("      \"rationale\": \"string|null\",");
        sb.AppendLine("      \"tags\": [\"string\"]");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"warnings\": [\"string\"]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- action=Skip if unsure.");
        sb.AppendLine("- targetRelativePath must be relative (no drive letters, no leading slash, no '..').");
        sb.AppendLine("- newFileName must be a filename only (no directories).");
        sb.AppendLine("- confidence0to1 must be between 0 and 1.");

        return sb.ToString();
    }

    public static string BuildPlannerUserPrompt(
        string destinationRootName,
        IReadOnlyList<FileContext> contexts)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Destination root name: {destinationRootName}");
        sb.AppendLine("Propose where each sourcePath should go under the destination root name.");
        sb.AppendLine("Consider file extension, content previews, and metadata.");
        sb.AppendLine();
        sb.AppendLine("Files:");
        for (var i = 0; i < contexts.Count; i++)
        {
            var c = contexts[i];
            sb.AppendLine($"[{i + 1}] sourcePath: {c.SourcePath}");
            sb.AppendLine($"    fileName: {c.FileName}");
            sb.AppendLine($"    extension: {c.Extension}");
            sb.AppendLine($"    sizeBytes: {c.SizeBytes}");
            sb.AppendLine($"    lastWriteTimeUtc: {c.LastWriteTimeUtc:O}");
            if (!string.IsNullOrWhiteSpace(c.MimeType))
                sb.AppendLine($"    mimeType: {c.MimeType}");
            if (!string.IsNullOrWhiteSpace(c.TextPreview))
                sb.AppendLine($"    textPreview: {c.TextPreview}");
            if (c.ImageWidth is not null && c.ImageHeight is not null)
                sb.AppendLine($"    image: {c.ImageWidth}x{c.ImageHeight} (thumbnail included separately if supported)");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}


