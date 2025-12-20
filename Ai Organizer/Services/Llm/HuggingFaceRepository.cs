using Ai_Organizer.Models.Settings;
using Ai_Organizer.Services.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Llm;

public sealed class HuggingFaceRepository : IModelRepository
{
    private readonly AppSettingsService _settings;
    private readonly ISecretStore _secrets;
    private readonly IHttpClientFactory _httpClientFactory;

    public HuggingFaceRepository(AppSettingsService settings, ISecretStore secrets, IHttpClientFactory httpClientFactory)
    {
        _settings = settings;
        _secrets = secrets;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => "Hugging Face";
    public RepositoryType Type => RepositoryType.HuggingFace;
    public bool Enabled => true;

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken)
    {
        var s = await _settings.GetAsync(cancellationToken);
        if (!s.HuggingFace.Enabled)
            return Array.Empty<string>();

        var token = await _secrets.GetAsync(SecretKeys.HuggingFaceToken, cancellationToken);
        var client = _httpClientFactory.CreateClient();
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Simple list; HF can be large, so cap to first 200.
        var url = s.HuggingFace.Endpoint.TrimEnd('/') + "?limit=200";
        using var resp = await client.GetAsync(url, cancellationToken);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var list = new List<string>();
        foreach (var m in doc.RootElement.EnumerateArray())
        {
            if (m.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                list.Add(id.GetString() ?? "");
        }

        return list.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    }
}

