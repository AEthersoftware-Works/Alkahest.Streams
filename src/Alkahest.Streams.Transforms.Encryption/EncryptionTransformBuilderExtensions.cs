namespace Alkahest.Streams;

using Transforms.Encryption;

/// <summary>
/// Extension methods for adding encryption and decryption handlers to the transform builder.
/// </summary>
public static class EncryptionTransformBuilderExtensions
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds an encryption handler of the specified type with default options.
        /// </summary>
        /// <typeparam name="THandler">The type of encryption handler.</typeparam>
        /// <returns>The builder instance for chaining.</returns>
        public ITransformBuilder EncryptWith<THandler>()
            where THandler : EncryptionHandler, ITransformHandler, new()
            => builder.ProcessWith<THandler>();

        /// <summary>
        /// Adds an encryption handler with specified options.
        /// </summary>
        /// <typeparam name="THandler">The type of encryption handler.</typeparam>
        /// <typeparam name="THandlerOptions">The options type.</typeparam>
        /// <param name="options">The handler options.</param>
        /// <returns>The builder instance for chaining.</returns>
        public ITransformBuilder EncryptWith<THandler, THandlerOptions>(THandlerOptions options)
            where THandler : EncryptionHandler, ITransformHandler, new()
            where THandlerOptions : new()
            => builder.ProcessWith<THandler, THandlerOptions>(options);

        /// <summary>
        /// Adds an encryption handler with specified options.
        /// </summary>
        /// <typeparam name="THandler">The type of encryption handler.</typeparam>
        /// <typeparam name="THandlerOptions">The options type.</typeparam>
        /// <param name="configure">An optional delegate that configures the <typeparamref name="THandlerOptions"/> instance before it's used by the handler. This allows customization of settings in a fluent manner similar to dependency injection configuration patterns.</param>
        /// <returns>The builder instance for chaining.</returns>
        public ITransformBuilder EncryptWith<THandler, THandlerOptions>(Action<THandlerOptions>? configure)
            where THandler : EncryptionHandler, ITransformHandler, new()
            where THandlerOptions : new()
            => builder.ProcessWith<THandler, THandlerOptions>(configure);

        /// <summary>
        /// Adds a decryption handler of the specified type with default options.
        /// </summary>
        /// <typeparam name="THandler">The type of decryption handler.</typeparam>
        /// <returns>The builder instance for chaining.</returns>
        public ITransformBuilder DecryptWith<THandler>()
            where THandler : DecryptionHandler, ITransformHandler, new()
            => builder.ProcessWith<THandler>();

        /// <summary>
        /// Adds a decryption handler with specified options.
        /// </summary>
        /// <typeparam name="THandler">The type of decryption handler.</typeparam>
        /// <typeparam name="THandlerOptions">The options type.</typeparam>
        /// <param name="options">The handler options.</param>
        /// <returns>The builder instance for chaining.</returns>
        public ITransformBuilder DecryptWith<THandler, THandlerOptions>(THandlerOptions options)
            where THandler : DecryptionHandler, ITransformHandler, new()
            where THandlerOptions : new()
            => builder.ProcessWith<THandler, THandlerOptions>(options);

        /// <summary>
        /// Adds a decryption handler with specified options.
        /// </summary>
        /// <typeparam name="THandler">The type of decryption handler.</typeparam>
        /// <typeparam name="THandlerOptions">The options type.</typeparam>
        /// <param name="configure">An optional delegate that configures the <typeparamref name="THandlerOptions"/> instance before it's used by the handler. This allows customization of settings in a fluent manner similar to dependency injection configuration patterns.</param>
        /// <returns>The builder instance for chaining.</returns>
        public ITransformBuilder DecryptWith<THandler, THandlerOptions>(Action<THandlerOptions>? configure)
           where THandler : DecryptionHandler, ITransformHandler, new()
           where THandlerOptions : new()
            => builder.ProcessWith<THandler, THandlerOptions>(configure);
    }
}