using SkiaSharp;

namespace Alkahest.Streams.Extensions.SkiaSharp.Adjustments;

/// <summary>
/// Defines an adjustment that can be applied to an image bitmap.
/// </summary>
public interface ISkiaSharpImageAdjustment
{
    /// <summary>
    /// Applies the adjustment to the specified bitmap.
    /// </summary>
    /// <param name="bitmap">The bitmap to adjust.</param>
    void Apply(SKBitmap bitmap);
}
