using System;
using System.Collections.Generic;

namespace Ai_Organizer.Services.Extraction;

public static class MimeTypes
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Text
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".json"] = "application/json",
        [".csv"] = "text/csv",
        [".log"] = "text/plain",

        // Images
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
    };

    public static string FromExtension(string extensionWithDot)
    {
        if (string.IsNullOrWhiteSpace(extensionWithDot))
            return "application/octet-stream";

        return Map.TryGetValue(extensionWithDot, out var mime)
            ? mime
            : "application/octet-stream";
    }
}


