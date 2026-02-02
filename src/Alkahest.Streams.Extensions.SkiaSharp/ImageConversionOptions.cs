namespace Alkahest.Streams.Extensions.SkiaSharp;

using Adjustments;

/// <summary>
/// Options for configuring image conversion, including quality, formats, and adjustments.
/// </summary>
/// <param name="quality">The compression quality level (0-100).</param>
/// <param name="inputFormat">The expected input image format.</param>
/// <param name="outputFormat">The desired output image format.</param>
public class ImageConversionOptions(
    int quality = 90,
    ImageFormat inputFormat = ImageFormat.Unknown,
    ImageFormat outputFormat = ImageFormat.Webp)
{
    /// <summary>
    /// Initializes a new instance with default values: quality 90, unknown input, WebP output.
    /// </summary>
    public ImageConversionOptions() : this(90, ImageFormat.Unknown, ImageFormat.Webp)
    { }

    /// <summary>
    /// Gets or sets the compression quality (0-100).
    /// </summary>
    public int Quality
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(value));

            field = value;
        }
    } = quality;

    /// <summary>
    /// Gets or sets the expected input image format.
    /// </summary>
    public ImageFormat InputFormat { get; set; } = inputFormat;

    /// <summary>
    /// Gets or sets the output image format.
    /// </summary>
    public ImageFormat OutputFormat { get; set; } = outputFormat;

    // Stack of actions
    /// <summary>
    /// Gets the list of image adjustments to apply.
    /// </summary>
    public List<ISkiaSharpImageAdjustment> Adjustments { get; } = [];

    /// <summary>
    /// Adds an adjustment to the list and returns this for chaining.
    /// </summary>
    /// <param name="adjustment">The adjustment to add.</param>
    /// <returns>This instance for fluent API.</returns>
    public ImageConversionOptions AddAdjustment(ISkiaSharpImageAdjustment adjustment)
    {
        Adjustments.Add(adjustment);
        return this;  // Fluent chaining
    }

    /// <summary>
    /// Resets options to default values.
    /// </summary>
    public void Reset()
    {
        Quality = 90;
        InputFormat = ImageFormat.Unknown;
        OutputFormat = ImageFormat.Webp;
        Adjustments.Clear();
    }
}