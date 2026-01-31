namespace Alkahest.Streams.Transforms.Encryption;

/// <summary>
/// Provides extension methods for <see cref="ITransformBuilder"/> to add AES-GCM encryption and decryption handlers.
/// </summary>
public static class TransformBuilderExtensions
{
    extension(ITransformBuilder builder)
    {
        /// <summary>
        /// Adds an AES-256-GCM encryption handler to the transform builder with the specified key and options.
        /// </summary>
        /// <param name="key">The 32-byte encryption key for AES-256.</param>
        /// <param name="aad">Optional additional authenticated data for integrity verification.</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder EncryptWithAesGcm(
            byte[] key,
            byte[]? aad = null
        ) => builder.EncryptWith<AesGcmEncryptionHandler, AesGcmEncryptionOptions>(
            new AesGcmEncryptionOptions()
            {
                Key = key,
                Aad = aad
            });

        /// <summary>
        /// Adds an AES-256-GCM decryption handler to the transform builder with the specified key and options.
        /// </summary>
        /// <param name="key">The 32-byte decryption key for AES-256.</param>
        /// <param name="aad">Optional additional authenticated data for integrity verification.</param>
        /// <returns>The transform builder for chaining.</returns>
        public ITransformBuilder DecryptWithAesGcm(
            byte[] key,
            byte[]? aad = null
        ) => builder.DecryptWith<AesGcmDecryptionHandler, AesGcmEncryptionOptions>(
            new AesGcmEncryptionOptions()
            {
                Key = key,
                Aad = aad
            });
    }
}