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

public sealed class AnthropicProvider : ILLMProvider, IModelRepository
{
    private readonly AppSettingsService _settings;
    private readonly ISecretStore _secrets;
    private readonly IHttpClientFactory _httpClientFactory;

    public AnthropicProvider(AppSettingsService settings, ISecretStore secrets, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _secrets = secrets;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => "Anthropic";
    public RepositoryType Type => RepositoryType.Anthropic;
    public bool Enabled => true;
    public bool SupportsVision => true;

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken)
    {
        // Anthropic doesn't have a public model listing API, so return known models
        return new[] { "claude-3-opus-20250219", "claude-3-sonnet-20250219", "claude-3-haiku-20250307" };
    }

    public async Task<string> ChatJsonAsync(ChatJsonRequest request, CancellationToken cancellationToken)
    {
        var apiKey = await _secrets.GetAsync(SecretKeys.AnthropicApiKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic API key is not set.");

        var client = _httpClientFactory.CreateClient();
        var url = "https://api.anthropic.com/v1/messages";

        var payload = new
        {
            model = request.Model,
            max_tokens = 4096,
            system = request.SystemPrompt,
            messages = new object[]
            {
                new { role = "user", content = request.UserPrompt }
            }
        };

        var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        
        httpReq.Headers.Add("x-api-key", apiKey);
        httpReq.Headers.Add("anthropic-version", "2023-06-01");

        using var resp = await client.SendAsync(httpReq, cancellationToken);
        resp.EnsureSuccessStatusCode();

        using var doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        if (doc is null)
            throw new InvalidOperationException("Empty Anthropic response.");

        var content = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return content ?? "";
    }
}

