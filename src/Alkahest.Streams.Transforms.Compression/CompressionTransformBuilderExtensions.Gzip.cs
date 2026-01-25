using System.IO.Compression;

namespace Alkahest.Streams;

using Transforms.Compression;

public static partial class CompressionTransformBuilderExtensions
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds a GZip compression handler with default options to the transform builder.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder CompressWithGzip()
        {
            builder.AddStreamHandler<GzipCompressionHandler>();
            return builder;
        }

        /// <summary>
        /// Adds a GZip compression handler with specified options to the transform builder.
        /// </summary>
        /// <param name="options">The compression options.</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder CompressWithGzip(ZLibCompressionOptions options)
        {
            builder.AddStreamHandler<GzipCompressionHandler, ZLibCompressionOptions>(options);
            return builder;
        }

        /// <summary>
        /// Adds a GZip decompression handler to the transform builder.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder DecompressWithGzip()
        {
            builder.AddStreamHandler<GzipDecompressionHandler>();
            return builder;
        }
    }
}

