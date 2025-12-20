namespace Ai_Organizer.Models.Extraction;

public sealed class ExtractorOptions
{
    public int MaxTextBytes { get; init; } = 64 * 1024;
    public int MaxTextChars { get; init; } = 6_000;

    /// <summary>
    /// Maximum dimension (width or height) for thumbnails.
    /// </summary>
    public int MaxImageDimension { get; init; } = 384;

    public bool IncludeText { get; init; } = true;
    public bool IncludeImageThumbnail { get; init; } = true;
}


