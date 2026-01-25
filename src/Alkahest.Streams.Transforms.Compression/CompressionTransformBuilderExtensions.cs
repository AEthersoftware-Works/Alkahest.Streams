namespace Alkahest.Streams;

using Transforms.Compression;

/// <summary>
/// 
/// </summary>
public static partial class CompressionTransformBuilderExtensions
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds a compression handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler">The type of compression handler.</typeparam>
        /// <returns>The builder instance.</returns>
        public ITransformBuilder CompressWith<THandler>() where THandler : CompressionHandler, ITransformHandler, new()
        {
            builder.AddStreamHandler<THandler>();
            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <typeparam name="THandlerOptions"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public ITransformBuilder CompressWith<THandler, THandlerOptions>(THandlerOptions options)
            where THandler : CompressionHandler, ITransformHandler, new()
            where THandlerOptions : class
        {
            builder.AddStreamHandler<THandler, THandlerOptions>(options);
            return builder;
        }

        /// <summary>
        /// Adds a decompression handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler">The type of decompression handler.</typeparam>
        /// <returns>The builder instance.</returns>
        public ITransformBuilder DecompressWith<THandler>() where THandler : DecompressionHandler, ITransformHandler, new()
        {
            builder.AddStreamHandler<THandler>();
            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <typeparam name="THandlerOptions"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public ITransformBuilder DecompressWith<THandler, THandlerOptions>(THandlerOptions options)
            where THandler : DecompressionHandler, ITransformHandler, new()
            where THandlerOptions : class
        {
            builder.AddStreamHandler<THandler, THandlerOptions>(options);
            return builder;
        }

        /// <summary>
        /// Adds a Brotli compression handler to the transform builder.
        /// </summary>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder CompressWithBrotli()
        {
            builder.AddStreamHandler<BrotliCompressionHandler>();
            return builder;
        }
    }
}
