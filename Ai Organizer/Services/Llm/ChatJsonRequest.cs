using System.Collections.Generic;

namespace Ai_Organizer.Services.Llm;

public sealed class ChatJsonRequest
{
    public string Model { get; init; } = "";
    public string SystemPrompt { get; init; } = "";
    public string UserPrompt { get; init; } = "";

    /// <summary>
    /// Optional base64-encoded PNG thumbnails. Providers may ignore if unsupported.
    /// </summary>
    public IReadOnlyList<string> ImagePngBase64 { get; init; } = new List<string>();
}


