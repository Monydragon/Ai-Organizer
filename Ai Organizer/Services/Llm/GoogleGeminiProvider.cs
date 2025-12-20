using Ai_Organizer.Services.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Llm;

public sealed class GoogleGeminiProvider : ILLMProvider, IModelRepository
{
    private readonly AppSettingsService _settings;
    private readonly ISecretStore _secrets;
    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleGeminiProvider(AppSettingsService settings, ISecretStore secrets, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _secrets = secrets;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => "Google Gemini";
    public RepositoryType Type => RepositoryType.GoogleGemini;
    public bool Enabled => true;
    public bool SupportsVision => true;

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken)
    {
        var apiKey = await _secrets.GetAsync(SecretKeys.GoogleGeminiApiKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return Array.Empty<string>();

        var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={Uri.EscapeDataString(apiKey)}";

        try
        {
            using var resp = await client.GetAsync(url, cancellationToken);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var list = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in models.EnumerateArray())
                {
                    if (m.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                    {
                        var nameStr = name.GetString() ?? "";
                        // Google returns "models/gemini-pro", we want just the model id
                        if (nameStr.StartsWith("models/"))
                            nameStr = nameStr.Substring(7);
                        if (!string.IsNullOrWhiteSpace(nameStr))
                            list.Add(nameStr);
                    }
                }
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public async Task<string> ChatJsonAsync(ChatJsonRequest request, CancellationToken cancellationToken)
    {
        var apiKey = await _secrets.GetAsync(SecretKeys.GoogleGeminiApiKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Google Gemini API key is not set.");

        var client = _httpClientFactory.CreateClient();
        var modelId = request.Model.StartsWith("models/") ? request.Model : $"models/{request.Model}";
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new { text = $"{request.SystemPrompt}\n\n{request.UserPrompt}" }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.2
            }
        };

        using var resp = await client.PostAsJsonAsync(url, payload, cancellationToken);
        resp.EnsureSuccessStatusCode();

        using var doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (doc is null)
            throw new InvalidOperationException("Empty Gemini response.");

        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("No candidates in Gemini response.");

        var content = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return content ?? "";
    }
}

