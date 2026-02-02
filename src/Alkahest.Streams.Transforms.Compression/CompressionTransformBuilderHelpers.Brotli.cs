using System.IO.Compression;

namespace Alkahest.Streams;

using Transforms.Compression;

public static partial class CompressionTransformBuilderHelpers
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds a Brotli compression handler with specified options to the transform builder.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder CompressWithBrotli()
            => builder.ProcessWith<BrotliCompressionHandler>();

#if NET10_0_OR_GREATER
        // BrotliCompressionOptions introduced in .NET 10+

        /// <summary>
        /// Adds a Brotli compression handler with specified options to the transform builder.
        /// </summary>
        /// <param name="options">The compression options.</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder CompressWithBrotli(BrotliCompressionOptions options)
            => builder.CompressWith<BrotliCompressionHandler, BrotliCompressionOptions>(options);

        /// <summary>
        /// Adds a Brotli compression handler with specified options to the transform builder.
        /// </summary>
        /// <param name="configure">An optional delegate that configures the <see cref="BrotliCompressionOptions"/> instance before it's used by the handler. This allows customization of compression settings, such as quality level or buffer sizes, in a fluent manner similar to dependency injection configuration patterns.</param>
        /// <returns></returns>
        public ITransformBuilder CompressWithBrotli(Action<BrotliCompressionOptions>? configure) 
            => builder.CompressWith<BrotliCompressionHandler, BrotliCompressionOptions>(configure);

#endif
        /// <summary>
        /// Adds a Brotli decompression handler to the transform builder.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder DecompressWithBrotli() 
            => builder.DecompressWith<BrotliDecompressionHandler>();
    }
}

