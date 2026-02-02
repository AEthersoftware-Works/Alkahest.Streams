using SkiaSharp;

namespace Alkahest.Streams.Extensions.SkiaSharp.Adjustments;

/// <summary>
/// Adjustment that resizes the image to specified dimensions.
/// </summary>
public class ResizeAdjustment : ISkiaSharpImageAdjustment
{
    /// <summary>
    /// Target width in pixels (0 to use original).
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Target height in pixels (0 to use original).
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Whether to preserve the aspect ratio during resize.
    /// </summary>
    public bool PreserveAspect { get; set; } = true;

    /// <summary>
    /// Resizes the bitmap according to the specified dimensions and options.
    /// </summary>
    /// <param name="bitmap">The bitmap to resize.</param>
    public void Apply(SKBitmap bitmap)
    {
        int w = Width > 0 ? Width : bitmap.Width;
        int h = Height > 0 ? Height : bitmap.Height;

        if (PreserveAspect && (Width > 0 ^ Height > 0))
        {
            float ratio = (float)bitmap.Width / bitmap.Height;
            if (Width > 0)
                h = (int)(w / ratio);
            else
                w = (int)(h * ratio);
        }

        // Recommended "High Quality" Equivalent.
        var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

        var resized = bitmap.Resize(new SKImageInfo(w, h), sampling);
        bitmap.InstallPixels(resized.Info, resized.GetPixels());  // Mutate in place
    }
}
