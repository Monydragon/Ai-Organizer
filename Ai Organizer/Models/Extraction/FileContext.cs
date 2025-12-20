using System;

namespace Ai_Organizer.Models.Extraction;

public sealed class FileContext
{
    public required string SourcePath { get; init; }
    public required string FileName { get; init; }
    public required string Extension { get; init; }

    public long SizeBytes { get; set; }
    public DateTimeOffset LastWriteTimeUtc { get; set; }

    public string? MimeType { get; set; }

    // Text extraction
    public string? TextPreview { get; set; }

    // Image extraction
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public string? ThumbnailPngBase64 { get; set; }
}


