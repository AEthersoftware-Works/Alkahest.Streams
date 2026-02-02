using System.Buffers;

namespace Alkahest.Streams.Extensions.SkiaSharp;

/// <summary>
/// Provides functionality to detect the format of an image from a stream by examining its header (magic bytes).
/// Supports various formats including PNG, JPEG, GIF, WebP, BMP, ICO, HEIF/HEIC/AVIF, KTX, ASTC, DNG, PKM, and WBMP.
/// </summary>
public class ImageFormatDetector
{
    /// <summary>
    /// Detects the image format by reading the beginning of the stream.
    /// </summary>
    /// <param name="stream">The input stream to detect format from.</param>
    /// <returns>The detected ImageFormat, or Unknown if not recognized.</returns>
    public static async ValueTask<ImageFormat> DetectAsync(Stream stream)
    {
        if (!stream.CanSeek || !stream.CanRead)
            return ImageFormat.Unknown;

        long originalPos = stream.Position;
        stream.Position = 0;

        var buffer = ArrayPool<byte>.Shared.Rent(32);
        try
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 32));

            stream.Position = originalPos; // Restore

            var span = buffer.AsSpan(0, bytesRead);

            // Png
            byte[] pngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            if (span.Length >= 8 && span.StartsWith(pngMagic))
                return ImageFormat.Png;

            // Jpeg
            byte[] jpegMagic = [0xFF, 0xD8, 0xFF];
            if (span.Length >= 3 && span.StartsWith(jpegMagic))
                return ImageFormat.Jpeg;

            // Gif
            byte[] gifMagic = [0x47, 0x49, 0x46, 0x38];
            if (span.Length >= 4 && span.StartsWith(gifMagic))
                return ImageFormat.Gif;

            // WebP
            byte[] webpMagic = [0x52, 0x49, 0x46, 0x46];
            if (span.Length >= 12 && span.Slice(0, 4).SequenceEqual(webpMagic) &&
                span.Slice(8, 4).SequenceEqual("WEBP"u8))
                return ImageFormat.Webp;

            // Bmp
            byte[] bmpMagic = [0x42, 0x4D];
            if (span.Length >= 2 && span.StartsWith(bmpMagic))
                return ImageFormat.Bmp;

            // Ico
            byte[] icoMagic = [0x00, 0x00, 0x01, 0x00];
            if (span.Length >= 4 && span.StartsWith(icoMagic))
                return ImageFormat.Ico;

            // Heif/Heic/Avif (ftyp + brand)
            if (span.Length >= 12 && span.Slice(4, 4).SequenceEqual("ftyp"u8))
            {
                var brand = span.Slice(8, 4);
                if (brand.SequenceEqual("avif"u8)) return ImageFormat.Avif;
                if (brand.SequenceEqual("heic"u8) || brand.SequenceEqual("heix"u8) || brand.SequenceEqual("hevc"u8) || brand.SequenceEqual("mif1"u8))
                    return ImageFormat.Heif;
            }

            // Ktx
            byte[] ktxMagic = [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];
            if (span.Length >= 12 && span.StartsWith(ktxMagic))
                return ImageFormat.Ktx;

            // Astc
            byte[] astcMagic = [0x13, 0xAB, 0xA1, 0x5C];
            if (span.Length >= 4 && span.StartsWith(astcMagic))
                return ImageFormat.Astc;

            // Dng (TIFF-based)
            byte[] tiffMagic = [0x49, 0x49, 0x2A, 0x00];
            byte[] tiff2Magic = [0x4D, 0x4D, 0x00, 0x2A];
            if (span.Length >= 4 && (span.StartsWith(tiffMagic) || span.StartsWith(tiff2Magic)))
                return ImageFormat.Dng;

            // Pkm / Wbmp (less common, basic check)
            byte[] pkmMagic = [0x50, 0x4B, 0x4D, 0x20];
            if (span.Length >= 4 && span.StartsWith(pkmMagic))
                return ImageFormat.Pkm;

            byte[] wbmpMagic = [0x00, 0x00];
            byte[] wbmp2Magic = [0x01, 0x00];
            if (span.Length >= 2 && (span.StartsWith(wbmpMagic) || span.StartsWith(wbmp2Magic)))
                return ImageFormat.Wbmp;

            return ImageFormat.Unknown;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            stream.Position = 0; // Reset for further processing
        }
    }
}

