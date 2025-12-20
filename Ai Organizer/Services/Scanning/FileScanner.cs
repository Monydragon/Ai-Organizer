using Ai_Organizer.Models.Scanning;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Scanning;

public sealed class FileScanner
{
    public async Task<IReadOnlyList<FileCandidate>> ScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (options.Roots.Count == 0)
            return Array.Empty<FileCandidate>();

        var matcher = BuildMatcher(options.IncludeGlob, options.ExcludeGlob);

        var results = new List<FileCandidate>(capacity: 1024);
        var dirsVisited = 0;
        var filesVisited = 0;
        var filesMatched = 0;

        foreach (var root in options.Roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(root) && !File.Exists(root))
                continue;

            // If user added a file directly, treat as a single candidate (still apply globs).
            if (File.Exists(root))
            {
                var filePath = root;
                var rootDir = Path.GetDirectoryName(filePath) ?? "";
                var rel = Path.GetFileName(filePath);
                filesVisited++;

                if (ShouldIncludeFile(filePath, rootDir, rel, options, matcher, cancellationToken, out var candidate))
                {
                    results.Add(candidate);
                    filesMatched++;
                }

                progress?.Report(new ScanProgress(root, dirsVisited, filesVisited, filesMatched));
                continue;
            }

            // Directory root
            var stack = new Stack<(string dir, int depth)>();
            stack.Push((root, 0));

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (dir, depth) = stack.Pop();
                dirsVisited++;

                if (!options.IncludeHidden && IsHiddenOrSystem(dir))
                    continue;

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(dir);
                }
                catch
                {
                    // Permission or IO errors shouldn't kill the scan.
                    continue;
                }

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    filesVisited++;

                    var rel = SafeGetRelativePath(root, file);
                    if (ShouldIncludeFile(file, root, rel, options, matcher, cancellationToken, out var candidate))
                    {
                        results.Add(candidate);
                        filesMatched++;
                    }

                    if (filesVisited % 250 == 0)
                        progress?.Report(new ScanProgress(root, dirsVisited, filesVisited, filesMatched));
                }

                if (depth >= options.MaxDepth)
                    continue;

                IEnumerable<string> subDirs;
                try
                {
                    subDirs = Directory.EnumerateDirectories(dir);
                }
                catch
                {
                    continue;
                }

                foreach (var subDir in subDirs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!options.IncludeHidden && IsHiddenOrSystem(subDir))
                        continue;

                    stack.Push((subDir, depth + 1));
                }

                progress?.Report(new ScanProgress(root, dirsVisited, filesVisited, filesMatched));
            }
        }

        // Yield once in case caller invoked from UI thread (helps avoid long sync continuation chains).
        await Task.Yield();
        return results;
    }

    private static bool ShouldIncludeFile(
        string filePath,
        string root,
        string relativePath,
        ScanOptions options,
        Matcher matcher,
        CancellationToken cancellationToken,
        out FileCandidate candidate)
    {
        candidate = null!;
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var attrs = File.GetAttributes(filePath);
            if (!options.IncludeHidden && (attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System)))
                return false;

            var info = new FileInfo(filePath);
            if (options.MinSizeBytes is not null && info.Length < options.MinSizeBytes.Value)
                return false;
            if (options.MaxSizeBytes is not null && info.Length > options.MaxSizeBytes.Value)
                return false;

            // Glob matching works on normalized relative paths.
            var rel = NormalizeGlobPath(relativePath);
            if (!matcher.Match(rel).HasMatches)
                return false;

            candidate = new FileCandidate
            {
                RootPath = root,
                FullPath = filePath,
                RelativePath = relativePath,
                Name = Path.GetFileName(filePath),
                Extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),
                SizeBytes = info.Length,
                LastWriteTime = info.LastWriteTimeUtc,
                Attributes = attrs
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Matcher BuildMatcher(string includeGlob, string excludeGlob)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

        var includes = SplitPatterns(includeGlob);
        if (includes.Count == 0)
            includes.Add("**/*");
        foreach (var p in includes)
            matcher.AddInclude(p);

        foreach (var p in SplitPatterns(excludeGlob))
            matcher.AddExclude(p);

        return matcher;
    }

    private static List<string> SplitPatterns(string patterns)
    {
        return patterns
            .Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }

    private static bool IsHiddenOrSystem(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            return attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System);
        }
        catch
        {
            return false;
        }
    }

    private static string SafeGetRelativePath(string root, string fullPath)
    {
        try
        {
            return Path.GetRelativePath(root, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }

    private static string NormalizeGlobPath(string relativePath) => relativePath.Replace('\\', '/');
}


