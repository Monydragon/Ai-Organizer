using Ai_Organizer.Models.Extraction;
using Ai_Organizer.Models.Scanning;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ai_Organizer.Services.Extraction;

public sealed class ImageThumbnailEnricher : IFileContextEnricher
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif"
    };

    public bool CanHandle(FileCandidate candidate)
    {
        var ext = Path.GetExtension(candidate.FullPath);
        return ImageExtensions.Contains(ext);
    }

    public async Task EnrichAsync(FileCandidate candidate, FileContext context, ExtractorOptions options, CancellationToken cancellationToken)
    {
        if (!options.IncludeImageThumbnail)
            return;

        try
        {
            // Skia decode (sync). We still honor cancellation before/after.
            cancellationToken.ThrowIfCancellationRequested();

            await using var fs = File.OpenRead(candidate.FullPath);
            using var managed = new SKManagedStream(fs);
            using var codec = SKCodec.Create(managed);
            if (codec is null)
                return;

            var srcInfo = codec.Info;
            context.ImageWidth = srcInfo.Width;
            context.ImageHeight = srcInfo.Height;

            var maxDim = Math.Max(1, options.MaxImageDimension);
            var scale = Math.Min((float)maxDim / srcInfo.Width, (float)maxDim / srcInfo.Height);
            scale = Math.Min(scale, 1.0f);

            var dstW = Math.Max(1, (int)Math.Round(srcInfo.Width * scale));
            var dstH = Math.Max(1, (int)Math.Round(srcInfo.Height * scale));

            using var srcBitmap = SKBitmap.Decode(codec);
            if (srcBitmap is null)
                return;

            using var resized = srcBitmap.Resize(new SKImageInfo(dstW, dstH), SKFilterQuality.Medium);
            if (resized is null)
                return;

            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);

            cancellationToken.ThrowIfCancellationRequested();
            context.ThumbnailPngBase64 = Convert.ToBase64String(data.ToArray());
        }
        catch
        {
            // ignore
        }
    }
}


