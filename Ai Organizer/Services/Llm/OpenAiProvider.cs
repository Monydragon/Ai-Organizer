using Ai_Organizer.Services.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Llm;

public sealed class OpenAiProvider : ILLMProvider
{
    private readonly AppSettingsService _settings;
    private readonly ISecretStore _secrets;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiProvider(AppSettingsService settings, ISecretStore secrets, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _secrets = secrets;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => "OpenAI";
    public bool SupportsVision => true; // model-dependent

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken)
    {
        var apiKey = await _secrets.GetAsync(SecretKeys.OpenAiApiKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return Array.Empty<string>();

        var s = await _settings.GetAsync(cancellationToken);
        var baseUrl = s.OpenAi.Endpoint.TrimEnd('/');
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var resp = await client.GetAsync($"{baseUrl}/models", cancellationToken);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var list = new List<string>();
        foreach (var m in data.EnumerateArray())
        {
            if (m.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                list.Add(id.GetString() ?? "");
        }

        return list.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    }

    public async Task<string> ChatJsonAsync(ChatJsonRequest request, CancellationToken cancellationToken)
    {
        var apiKey = await _secrets.GetAsync(SecretKeys.OpenAiApiKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not set.");

        var s = await _settings.GetAsync(cancellationToken);
        var baseUrl = s.OpenAi.Endpoint.TrimEnd('/');
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Note: response_format json_object is supported on many modern models, but not all.
        // If unsupported, OpenAI will error; user can switch models.
        var payload = new
        {
            model = request.Model,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        using var resp = await client.PostAsJsonAsync($"{baseUrl}/chat/completions", payload, cancellationToken);
        resp.EnsureSuccessStatusCode();

        using var doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (doc is null)
            throw new InvalidOperationException("Empty OpenAI response.");

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? "";
    }
}


