using System.IO.Compression;

namespace Alkahest.Streams;

using Transforms.Compression;

public static partial class CompressionTransformBuilderExtensions
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds a Brotli compression handler with specified options to the transform builder.
        /// </summary>
        /// <param name="options">The compression options.</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder CompressWithBrotli(BrotliCompressionOptions options)
        {
            builder.AddStreamHandler<BrotliCompressionHandler, BrotliCompressionOptions>(options);
            return builder;
        }

        /// <summary>
        /// Adds a Brotli decompression handler to the transform builder.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder DecompressWithBrotli()
        {
            builder.AddStreamHandler<BrotliDecompressionHandler>();
            return builder;
        }
    }
}

