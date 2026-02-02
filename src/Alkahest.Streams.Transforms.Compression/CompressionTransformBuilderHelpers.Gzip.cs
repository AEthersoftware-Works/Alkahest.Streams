using System.IO.Compression;

namespace Alkahest.Streams;

using Transforms.Compression;

public static partial class CompressionTransformBuilderHelpers
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds a GZip compression handler with default options to the transform builder.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder CompressWithGzip() 
            => builder.CompressWith<GzipCompressionHandler>();
#if NET10_0_OR_GREATER
        // ZLibCompressionOptions introduced in .NET 10+

        /// <summary>
        /// Adds a GZip compression handler with specified options to the transform builder.
        /// </summary>
        /// <param name="options">The compression options.</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder CompressWithGzip(ZLibCompressionOptions options) 
            => builder.CompressWith<GzipCompressionHandler, ZLibCompressionOptions>(options);

        /// <summary>
        /// Add a GZip compress handler with the specified options to the transform builder.
        /// </summary>
        /// <param name="configure">An optional delegate that configures the <see cref="ZLibCompressionOptions"/> instance before it's used by the handler. This allows customization of compression settings, such as quality level or buffer sizes, in a fluent manner similar to dependency injection configuration patterns.</param>
        /// <returns></returns>
        public ITransformBuilder CompressWithGzip(Action<ZLibCompressionOptions>? configure) 
            => builder.CompressWith<GzipCompressionHandler, ZLibCompressionOptions>(configure);
#endif

        /// <summary>
        /// Adds a GZip decompression handler to the transform builder.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder DecompressWithGzip() 
            => builder.DecompressWith<GzipDecompressionHandler>();
    }
}

