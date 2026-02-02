using SkiaSharp;

namespace Alkahest.Streams.Extensions.SkiaSharp;

/// <summary>
/// Supported image formats
/// </summary>
public enum ImageFormat
{
    /// <summary>Unknown or unsupported format.</summary>
    Unknown = -2,
    /// <summary>Unspecified format.</summary>
    Unspecified = -1,
    /// <summary>Bitmap format.</summary>
    Bmp = 0,
    /// <summary>GIF format.</summary>
    Gif = 1,
    /// <summary>ICO format.</summary>
    Ico = 2,
    /// <summary>JPEG format.</summary>
    Jpeg = 3,
    /// <summary>PNG format.</summary>
    Png = 4,
    /// <summary>WBMP format.</summary>
    Wbmp = 5,
    /// <summary>WebP format.</summary>
    Webp = 6,
    /// <summary>PKM format.</summary>
    Pkm = 7,
    /// <summary>KTX format.</summary>
    Ktx = 8,
    /// <summary>ASTC format.</summary>
    Astc = 9,
    /// <summary>DNG format.</summary>
    Dng = 10,
    /// <summary>HEIF format.</summary>
    Heif = 11,
    /// <summary>AVIF format.</summary>
    Avif = 12,
    /// <summary>JPEG XL format.</summary>
    Jpegxl = 13,
}

/// <summary>
/// Extension methods for ImageFormat.
/// </summary>
public static class ImageFormatExtensions
{
    extension(ImageFormat imageFormat)
    {
        /// <summary>
        /// Converts the ImageFormat to the corresponding SkiaSharp encoded format.
        /// </summary>
        /// <returns>The SKEncodedImageFormat equivalent.</returns>
        public SKEncodedImageFormat AsSkEncodedImageFormat() => imageFormat switch
        {
            ImageFormat.Unknown => throw new InvalidCastException($"{nameof(ImageFormat.Unknown)} is not a valid {nameof(SKEncodedImageFormat)} value."),
            ImageFormat.Unspecified => SKEncodedImageFormat.Webp, // Default to Webp
            _ => (SKEncodedImageFormat)imageFormat
        };
    }
}
