namespace Ai_Organizer.Models.Settings;

public sealed class AppSettings
{
    public OllamaSettings Ollama { get; set; } = new();
    public OpenAiSettings OpenAi { get; set; } = new();

    /// <summary>
    /// Free-form prompt that becomes the default system instruction for planning.
    /// </summary>
    public string DefaultPrompt { get; set; } =
        "You are an assistant that organizes files. Return ONLY valid JSON matching the requested schema.";
}

public sealed class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
}

public sealed class OpenAiSettings
{
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Stored separately via secret store (not in plain app settings).
    /// </summary>
    public bool HasApiKey { get; set; }
}


