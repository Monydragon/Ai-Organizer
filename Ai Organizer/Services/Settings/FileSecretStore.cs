using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Settings;

/// <summary>
/// Stores secrets in a local file. On Windows, values are encrypted using DPAPI (CurrentUser).
/// On non-Windows platforms, values are stored in plain text (MVP) in the user's app data dir.
/// </summary>
public sealed class FileSecretStore : ISecretStore
{
    private readonly string _secretsPath;

    public FileSecretStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AiOrganizer");
        _secretsPath = Path.Combine(dir, "secrets.json");
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var dict = await LoadAsync(cancellationToken);
        if (!dict.TryGetValue(key, out var stored) || string.IsNullOrWhiteSpace(stored))
            return null;

        return TryDecryptIfNeeded(stored);
    }

    public async Task SetAsync(string key, string? value, CancellationToken cancellationToken)
    {
        var dict = await LoadAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            dict.Remove(key);
        }
        else
        {
            dict[key] = EncryptIfNeeded(value);
        }

        var dir = Path.GetDirectoryName(_secretsPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(_secretsPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
    }

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_secretsPath))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var json = await File.ReadAllTextAsync(_secretsPath, cancellationToken);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return dict ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string EncryptIfNeeded(string plaintext)
    {
        if (OperatingSystem.IsWindows())
        {
            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            return "dpapi:" + Convert.ToBase64String(protectedBytes);
        }

        return "plain:" + plaintext;
    }

    private static string? TryDecryptIfNeeded(string stored)
    {
        try
        {
            if (stored.StartsWith("dpapi:", StringComparison.OrdinalIgnoreCase))
            {
                if (!OperatingSystem.IsWindows())
                    return null;

                var b64 = stored["dpapi:".Length..];
                var protectedBytes = Convert.FromBase64String(b64);
                var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }

            if (stored.StartsWith("plain:", StringComparison.OrdinalIgnoreCase))
                return stored["plain:".Length..];

            // Back-compat: treat as plaintext
            return stored;
        }
        catch
        {
            return null;
        }
    }
}


