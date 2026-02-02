using SkiaSharp;

namespace Alkahest.Streams.Extensions.SkiaSharp.Adjustments;

/// <summary>
/// Adjustment that adds a text watermark to the image.
/// </summary>
public class WatermarkAdjustment : ISkiaSharpImageAdjustment
{
    /// <summary>
    /// The text to use as watermark.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The color of the watermark text.
    /// </summary>
    public SKColor Color { get; set; } = SKColors.White;

    /// <summary>
    /// The opacity of the watermark (0-1).
    /// </summary>
    public float Opacity { get; set; } = 0.5f;
    
    /// <summary>
    /// The font size of the watermark text.
    /// </summary>
    public float Size { get; set; } = 24f;
    
    /// <summary>
    /// The position to draw the watermark.
    /// </summary>
    public SKPoint Position { get; set; } = new SKPoint(10, 10);

    /// <summary>
    /// Applies the watermark to the bitmap by drawing the text.
    /// </summary>
    /// <param name="bitmap">The bitmap to adjust.</param>
    public void Apply(SKBitmap bitmap)
    {
        var font = new SKFont { Subpixel = true, Edging = SKFontEdging.SubpixelAntialias, Size = Size };
        using var paint = new SKPaint
        {
            Color = Color.WithAlpha((byte)(255 * Opacity)),
            IsAntialias = true
        };

        using var canvas = new SKCanvas(bitmap);
        canvas.DrawText(Text, Position.X, Position.Y, font, paint);
    }
}