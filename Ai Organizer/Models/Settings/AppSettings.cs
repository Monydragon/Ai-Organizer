namespace Ai_Organizer.Models.Settings;

public sealed class AppSettings
{
    public OllamaSettings Ollama { get; set; } = new();
    public OpenAiSettings OpenAi { get; set; } = new();
    public HuggingFaceSettings HuggingFace { get; set; } = new();
    public GoogleGeminiSettings GoogleGemini { get; set; } = new();
    public AnthropicSettings Anthropic { get; set; } = new();

    public string PreferredProvider { get; set; } = "Ollama";

    /// <summary>
    /// Free-form prompt that becomes the default system instruction for planning.
    /// </summary>
    public string DefaultPrompt { get; set; } =
        "You are an assistant that organizes files. Return ONLY valid JSON matching the requested schema.";
    
    /// <summary>
    /// Enable model performance tracking.
    /// </summary>
    public bool EnablePerformanceTracking { get; set; } = true;
}

public sealed class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
    public bool UseDocker { get; set; } = false;
    public string DockerImage { get; set; } = "ollama/ollama:latest";
    public string DockerContainerName { get; set; } = "ai-organizer-ollama";
    public int DockerPort { get; set; } = 11434;
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

public sealed class HuggingFaceSettings
{
    public string Endpoint { get; set; } = "https://huggingface.co/api/models";
    public string Model { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool HasToken { get; set; }
}

public sealed class GoogleGeminiSettings
{
    public string Model { get; set; } = "gemini-pro";
    public bool HasApiKey { get; set; }
}

public sealed class AnthropicSettings
{
    public string Model { get; set; } = "claude-3-sonnet-20250219";
    public bool HasApiKey { get; set; }
}

