namespace Alkahest.Streams;

public static class EncryptionTransformBuilderExtensions
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds an encryption handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler">The type of encryption handler.</typeparam>
        /// <returns>The builder instance.</returns>
        public ITransformBuilder EncryptWith<THandler>() where THandler : EncryptionHandler, ITransformHandler, new()
        {
            builder.AddStreamHandler<THandler>();
            return builder;
        }

        public ITransformBuilder EncryptWith<THandler, THandlerOptions>(THandlerOptions options)
            where THandler : EncryptionHandler, ITransformHandler, new()
            where THandlerOptions : class
        {
            builder.AddStreamHandler<THandler, THandlerOptions>(options);
            return builder;
        }

        /// <summary>
        /// Adds a decryption handler of the specified type.
        /// </summary>
        /// <typeparam name="THandler">The type of decryption handler.</typeparam>
        /// <returns>The builder instance.</returns>
        public ITransformBuilder DecryptWith<THandler>() where THandler : DecryptionHandler, ITransformHandler, new()
        {
            builder.AddStreamHandler<THandler>();
            return builder;
        }

        public ITransformBuilder DecryptWith<THandler, THandlerOptions>(THandlerOptions options)
            where THandler : DecryptionHandler, ITransformHandler, new()
            where THandlerOptions : class
        {
            builder.AddStreamHandler<THandler, THandlerOptions>(options);
            return builder;
        }
    }
}
