using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Llm;

public sealed class OllamaDockerService
{
    public async Task EnsureStartedAsync(string image, string containerName, int port, CancellationToken cancellationToken)
    {
        await PullAsync(image, cancellationToken);
        await RunAsync(image, containerName, port, cancellationToken);
    }

    private static async Task PullAsync(string image, CancellationToken cancellationToken)
    {
        // Best-effort pull; if already present, Docker exits quickly.
        await ExecAsync($"docker pull {image}", cancellationToken);
    }

    private static async Task RunAsync(string image, string containerName, int port, CancellationToken cancellationToken)
    {
        var args = $"docker rm -f {containerName}";
        await ExecAsync(args, cancellationToken, ignoreErrors: true);

        var runArgs = $"docker run -d --name {containerName} -p {port}:11434 {image}";
        await ExecAsync(runArgs, cancellationToken);
    }

    private static async Task ExecAsync(string command, CancellationToken cancellationToken, bool ignoreErrors = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        if (proc is null)
            throw new InvalidOperationException($"Failed to start process for: {command}");

        await proc.WaitForExitAsync(cancellationToken);
        if (proc.ExitCode != 0 && !ignoreErrors)
            throw new InvalidOperationException($"Command failed: {command}");
    }
}

