namespace Alkahest.Streams;

using Extensions.SkiaSharp;

/// <summary>
/// Extension methods for ITransformBuilder to add image reading and writing handlers with format-specific configurations.
/// </summary>
public static partial class ImageTransformBuilderExtensions
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds a handler to read PNG images.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder FromPng()
            => builder.ProcessWith<SkiaSharpImageReaderHandler, ImageConversionOptions>(
                new ImageConversionOptions { InputFormat = ImageFormat.Png }
            );

        /// <summary>
        /// Converts the image to PNG format with specified quality.
        /// </summary>
        /// <param name="quality">The quality level (0-100).</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder ConvertToPng(int quality = 90)
            => builder.ProcessWith<SkiaSharpImageWriterHandler, ImageConversionOptions>(
                new ImageConversionOptions {
                    OutputFormat = ImageFormat.Png,
                    Quality = quality
                }
            );

        /// <summary>
        /// Adds a handler to read JPEG images.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder FromJpg()
            => builder.ProcessWith<SkiaSharpImageReaderHandler, ImageConversionOptions>(
                new ImageConversionOptions { InputFormat = ImageFormat.Jpeg }
            );

        /// <summary>
        /// Converts the image to JPEG format with specified quality.
        /// </summary>
        /// <param name="quality">The quality level (0-100).</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder ConvertToJpeg(int quality = 85)
            => builder.ProcessWith<SkiaSharpImageWriterHandler, ImageConversionOptions>(
                new ImageConversionOptions
                {
                    OutputFormat = ImageFormat.Jpeg,
                    Quality = quality
                }
            );

        /// <summary>
        /// Adds a handler to read WebP images.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder FromWebp()
            => builder.ProcessWith<SkiaSharpImageReaderHandler, ImageConversionOptions>(
                new ImageConversionOptions { InputFormat = ImageFormat.Webp }
            );

        /// <summary>
        /// Converts the image to WebP format with specified quality.
        /// </summary>
        /// <param name="quality">The quality level (0-100).</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder ConvertToWebp(int quality = 90)
            => builder.ProcessWith<SkiaSharpImageWriterHandler, ImageConversionOptions>(
                new ImageConversionOptions
                {
                    OutputFormat = ImageFormat.Webp,
                    Quality = quality
                }
            );
    }
}
