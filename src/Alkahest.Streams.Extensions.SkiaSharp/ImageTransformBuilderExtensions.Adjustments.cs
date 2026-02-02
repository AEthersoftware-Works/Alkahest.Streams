namespace Alkahest.Streams;

using Extensions.SkiaSharp;
using Extensions.SkiaSharp.Adjustments;

public static partial class ImageTransformBuilderExtensions
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Gets the current image conversion options for the builder.
        /// </summary>
        public ImageConversionOptions ImageOptions
        {
            get
            {
                if (builder.ImageOptions == null)
                    return new();

                return builder.ImageOptions;
            }
        }

        /// <summary>
        /// Adds a resize adjustment to the image options.
        /// </summary>
        /// <param name="width">Target width in pixels.</param>
        /// <param name="height">Target height in pixels.</param>
        /// <param name="preserveAspect">Whether to maintain aspect ratio.</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder Resize(int width, int height, bool preserveAspect)
        {
            builder.ImageOptions.AddAdjustment(new ResizeAdjustment()
            {
                Width = width,
                Height = height,
                PreserveAspect = preserveAspect
            });
            return builder;
        }

        /// <summary>
        /// Adds a watermark text adjustment to the image options.
        /// </summary>
        /// <param name="text">The watermark text.</param>
        /// <param name="opacity">Opacity level (0-1).</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder AddWatermark(string text, float opacity = 0.5f)
        {
            builder.ImageOptions.AddAdjustment(new WatermarkAdjustment()
            {
                Text = text,
                Opacity = opacity
            });

            return builder;
        }

        /// <summary>
        /// Applies any pending image changes without format conversion.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder ApplyImageAdjustments()
        {
            builder.ImageOptions.OutputFormat = builder.ImageOptions.InputFormat;

            // Input - Ensure image format...
            builder.AddStreamHandler<SkiaSharpImageReaderHandler, ImageConversionOptions>(builder.ImageOptions);
            // Output - Processes adjustments
            builder.AddStreamHandler<SkiaSharpImageWriterHandler, ImageConversionOptions>(builder.ImageOptions);

            builder.ImageOptions.Reset();

            return builder;
        }

        /// <summary>
        /// Applies image changes with optional configuration.
        /// </summary>
        /// <param name="configure">Action to configure options.</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder ApplyImageAdjustments(Action<ImageConversionOptions>? configure = null)
        {
            configure?.Invoke(builder.ImageOptions);
            builder.ImageOptions.OutputFormat = builder.ImageOptions.InputFormat;

            // Input - Ensure image format...
            builder.AddStreamHandler<SkiaSharpImageReaderHandler, ImageConversionOptions>(builder.ImageOptions);
            // Output - Processes adjustments
            builder.AddStreamHandler<SkiaSharpImageWriterHandler, ImageConversionOptions>(builder.ImageOptions);

            builder.ImageOptions.Reset();

            return builder;
        }

    }
}

