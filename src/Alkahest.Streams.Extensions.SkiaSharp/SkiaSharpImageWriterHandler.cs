using SkiaSharp;

namespace Alkahest.Streams.Extensions.SkiaSharp;

/// <summary>
/// Handles image writing and transformation using SkiaSharp, applying adjustments and encoding to specified format.
/// </summary>
public class SkiaSharpImageWriterHandler : ImageWriterHandler
{
    /// <summary>
    /// Gets the name of this handler.
    /// </summary>
    public override string Name { get; init; } = nameof(ImageWriterHandler);

    private static readonly ImageConversionOptions DefaultOptions = new()
    {
        Quality = 90,
        OutputFormat = ImageFormat.Webp
    };

    /// <summary>
    /// Gets or sets the options for image conversion, including format, quality, and adjustments.
    /// </summary>
    public new IHandlerOptions<ImageConversionOptions> Options
    {
        get => (IHandlerOptions<ImageConversionOptions>)base.Options;
        set => base.Options = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkiaSharpImageWriterHandler"/> class.
    /// </summary>
    public SkiaSharpImageWriterHandler() => Options = HandlerOptions.Create(DefaultOptions);

    /// <summary>
    /// Processes the input stream by decoding the image, applying adjustments, and writing to the output stream in the specified format.
    /// </summary>
    /// <param name="input">The input stream containing the image data.</param>
    /// <param name="output">The output stream to write the transformed image.</param>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        // Keep the image format the same.
        if (Options.Value != null && Options.Value.InputFormat == ImageFormat.Unspecified && Options.Value.OutputFormat == ImageFormat.Unspecified)
        {
            Options.Value.OutputFormat = await ImageFormatDetector.DetectAsync(input);
        }

        using var bitmap = SKBitmap.Decode(input);
        if (bitmap == null)
            throw new InvalidDataException("Failed to decode image");

        foreach (var adjustment in Options.Value?.Adjustments ?? [])
        {
            adjustment.Apply(bitmap);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(
            (Options.Value?.OutputFormat ?? DefaultOptions.OutputFormat).AsSkEncodedImageFormat(),
            Options.Value?.Quality ?? DefaultOptions.Quality
        );

        await data.AsStream().CopyToAsync(output);
    }
}
