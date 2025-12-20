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

public sealed class OllamaProvider : ILLMProvider
{
    private readonly AppSettingsService _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public OllamaProvider(AppSettingsService settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => "Ollama";
    public bool SupportsVision => true; // model-dependent, but safe for UI capability

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken)
    {
        var s = await _settings.GetAsync(cancellationToken);
        var baseUrl = s.Ollama.Endpoint.TrimEnd('/');
        var client = _httpClientFactory.CreateClient();

        using var resp = await client.GetAsync($"{baseUrl}/api/tags", cancellationToken);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var list = new List<string>();
        foreach (var m in models.EnumerateArray())
        {
            if (m.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                list.Add(name.GetString() ?? "");
        }

        return list.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    }

    public async Task<string> ChatJsonAsync(ChatJsonRequest request, CancellationToken cancellationToken)
    {
        var s = await _settings.GetAsync(cancellationToken);
        var baseUrl = s.Ollama.Endpoint.TrimEnd('/');
        var client = _httpClientFactory.CreateClient();

        var payload = new
        {
            model = request.Model,
            stream = false,
            format = "json",
            messages = new object[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        using var resp = await client.PostAsJsonAsync($"{baseUrl}/api/chat", payload, cancellationToken);
        resp.EnsureSuccessStatusCode();

        using var doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (doc is null)
            throw new InvalidOperationException("Empty Ollama response.");

        if (!doc.RootElement.TryGetProperty("message", out var msg))
            throw new InvalidOperationException("Unexpected Ollama response: missing message.");

        if (!msg.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Unexpected Ollama response: missing message.content.");

        return content.GetString() ?? "";
    }
}


