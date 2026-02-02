namespace Alkahest.Streams.Extensions.SkiaSharp;

/// <summary>
/// Handles reading and validating image input streams based on expected format.
/// </summary>
public class SkiaSharpImageReaderHandler : ImageReaderHandler
{
    /// <inheritdoc/>
    public override string Name { get; init; } = nameof(ImageReaderHandler);

    private static readonly ImageConversionOptions DefaultOptions = new()
    {
        Quality = 0,
        InputFormat = ImageFormat.Webp
    };

    /// <summary>
    /// Gets or sets the options for image reading, primarily the expected input format.
    /// </summary>
    public new IHandlerOptions<ImageConversionOptions> Options
    {
        get => (IHandlerOptions<ImageConversionOptions>)base.Options;
        set => base.Options = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkiaSharpImageReaderHandler"/> class.
    /// </summary>
    public SkiaSharpImageReaderHandler() => Options = HandlerOptions.Create(DefaultOptions);

    /// <summary>
    /// Validates the input image format if specified and copies the stream to output.
    /// </summary>
    /// <param name="input">The input stream containing the image.</param>
    /// <param name="output">The output stream to copy to.</param>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        // If format is specified, detect to ensure input format is correct.
        if (Options.Value != null && Options.Value.InputFormat != ImageFormat.Unspecified)
        {
            var detected = await ImageFormatDetector.DetectAsync(input);
            if (detected != Options.Value.InputFormat)
            {
                throw new InvalidImageFormatException($"Expected {Options.Value.InputFormat} detected {detected}");
            }
        }

        // Copy the stream
        await input.CopyToAsync(output);
    }
}

