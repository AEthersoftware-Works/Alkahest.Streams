namespace Alkahest.Streams;

using Transforms.Compression;

/// <summary>
/// 
/// </summary>
public static partial class CompressionTransformBuilderHelpers
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds a compression handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler">The type of compression handler.</typeparam>
        /// <returns>The builder instance.</returns>
        public ITransformBuilder CompressWith<THandler>()
            where THandler : CompressionHandler, ITransformHandler, new()
            => builder.ProcessWith<THandler>();

        /// <summary>
        /// Adds a compression handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <typeparam name="THandlerOptions"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public ITransformBuilder CompressWith<THandler, THandlerOptions>(THandlerOptions options)
            where THandler : CompressionHandler, ITransformHandler, new()
            where THandlerOptions : new()
            => builder.ProcessWith<THandler, THandlerOptions>(options);

        /// <summary>
        /// Adds a compression handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <typeparam name="THandlerOptions"></typeparam>
        /// <param name="configure">An optional delegate that configures the <typeparamref name="THandlerOptions"/> instance before it's used by the handler. This allows customization of settings in a fluent manner similar to dependency injection configuration patterns.</param>
        /// <returns></returns>
        public ITransformBuilder CompressWith<THandler, THandlerOptions>(Action<THandlerOptions>? configure)
            where THandler : CompressionHandler, ITransformHandler, new()
            where THandlerOptions : class, new()
            => builder.ProcessWith<THandler, THandlerOptions>(configure);

        /// <summary>
        /// Adds a decompression handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler">The type of decompression handler.</typeparam>
        /// <returns>The builder instance.</returns>
        public ITransformBuilder DecompressWith<THandler>()
            where THandler : DecompressionHandler, ITransformHandler, new()
            => builder.ProcessWith<THandler>();

        /// <summary>
        /// Adds a decompression handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <typeparam name="THandlerOptions"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public ITransformBuilder DecompressWith<THandler, THandlerOptions>(THandlerOptions options)
            where THandler : DecompressionHandler, ITransformHandler, new()
            where THandlerOptions : new()
            => builder.ProcessWith<THandler, THandlerOptions>(options);

        /// <summary>
        /// Adds a decompression handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <typeparam name="THandlerOptions"></typeparam>
        /// <param name="configure">An optional delegate that configures the <typeparamref name="THandlerOptions"/> instance before it's used by the handler. This allows customization of settings in a fluent manner similar to dependency injection configuration patterns.</param>
        /// <returns></returns>
        public ITransformBuilder DecompressWith<THandler, THandlerOptions>(Action<THandlerOptions>? configure)
            where THandler : DecompressionHandler, ITransformHandler, new()
            where THandlerOptions : class, new()
            => builder.ProcessWith<THandler, THandlerOptions>(configure);
    }
}
