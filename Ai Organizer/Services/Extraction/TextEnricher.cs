using Ai_Organizer.Models.Extraction;
using Ai_Organizer.Models.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Extraction;

public sealed class TextEnricher : IFileContextEnricher
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".json", ".csv", ".log"
    };

    public bool CanHandle(FileCandidate candidate)
    {
        var ext = Path.GetExtension(candidate.FullPath);
        return TextExtensions.Contains(ext);
    }

    public async Task EnrichAsync(FileCandidate candidate, FileContext context, ExtractorOptions options, CancellationToken cancellationToken)
    {
        if (!options.IncludeText)
            return;

        try
        {
            await using var stream = File.OpenRead(candidate.FullPath);
            var max = Math.Max(0, options.MaxTextBytes);
            var buffer = new byte[max];
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

            var text = DecodeBestEffort(buffer.AsSpan(0, read));
            if (text.Length > options.MaxTextChars)
                text = text[..options.MaxTextChars];

            context.TextPreview = text;
        }
        catch
        {
            // ignore
        }
    }

    private static string DecodeBestEffort(ReadOnlySpan<byte> bytes)
    {
        // Basic UTF8 detection + replacement fallback.
        try
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return Sanitize(utf8.GetString(bytes));
        }
        catch
        {
            try
            {
                var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
                return Sanitize(utf8.GetString(bytes));
            }
            catch
            {
                return "";
            }
        }
    }

    private static string Sanitize(string s)
    {
        // Remove nulls which commonly indicate binary content.
        return s.Replace("\0", "");
    }
}


