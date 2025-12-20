using System;

namespace Ai_Organizer.Infrastructure;

public static class Formatters
{
    public static string Bytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public static string LocalTime(DateTimeOffset utc)
    {
        try
        {
            return utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        catch
        {
            return utc.ToString("u");
        }
    }
}


