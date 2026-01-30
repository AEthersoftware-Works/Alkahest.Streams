using System.Buffers;
using System.Security.Cryptography;

namespace Alkahest.Streams.Transforms.Encryption;

/// <summary>
/// Handles AES-GCM encryption of streams, providing authenticated encryption with integrity verification.
/// </summary>
public class AesGcmEncryptionHandler : EncryptionHandler
{
    private const int ChunkSize = 4096; // Balanced size for performance; adjust as needed

    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    public override string Name { get; init; } = nameof(AesGcmEncryptionHandler);

    /// <summary>
    /// Gets or sets the options for AesGcm encryption.
    /// </summary>
    public new IHandlerOptions<AesGcmEncryptionOptions> Options
    {
        get => (IHandlerOptions<AesGcmEncryptionOptions>)base.Options;
        set => base.Options = value;
    }

    /// <summary>
    /// Processes the input stream by encrypting it using AES-GCM and writing the encrypted data to the output stream.
    /// The nonce is written first, followed by ciphertext and authentication tags for each chunk.
    /// </summary>
    /// <param name="input">The input stream containing plaintext data to encrypt.</param>
    /// <param name="output">The output stream to write the encrypted data to.</param>
    /// <returns>A task representing the asynchronous encryption operation.</returns>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        ArgumentNullException.ThrowIfNull(Options.Value, nameof(Options.Value));

        var opts = Options.Value;

        if (opts.Key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes for AES-256-GCM");

        // Generate nonce
        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);

        // Write nonce first (decryption needs it)
        await output.WriteAsync(nonce);

        // Create AesGcm with explicit tag size (default 128 bits / 16 bytes)
        using var aesGcm = new AesGcm(opts.Key, opts.TagSizeInBytes);

        byte[] plaintextBuffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
        byte[] ciphertextBuffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
        byte[] tagBuffer = new byte[opts.TagSizeInBytes];

        try
        {
            int bytesRead;
            while ((bytesRead = await input.ReadAsync(plaintextBuffer.AsMemory(0, ChunkSize))) > 0)
            {
                // Encrypt the chunk
                aesGcm.Encrypt(
                    nonce.AsSpan(),
                    plaintextBuffer.AsSpan(0, bytesRead),
                    ciphertextBuffer.AsSpan(0, bytesRead),
                    tagBuffer.AsSpan(),
                    opts.Aad);

                // Write ciphertext + tag
                await output.WriteAsync(ciphertextBuffer.AsMemory(0, bytesRead));
                await output.WriteAsync(tagBuffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(plaintextBuffer);
            ArrayPool<byte>.Shared.Return(ciphertextBuffer);
            CryptographicOperations.ZeroMemory(nonce);
        }
    }
}

/// <summary>
/// Handles AES-GCM decryption of streams, providing authenticated decryption with integrity verification.
/// </summary>
public class AesGcmDecryptionHandler : DecryptionHandler
{
    private const int ChunkSize = 4096; // Balanced size for performance; adjust as needed

    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    public override string Name { get; init; } = nameof(AesGcmDecryptionHandler);

    /// <summary>
    /// Gets or sets the options for AesGcm decryption.
    /// </summary>
    public new IHandlerOptions<AesGcmEncryptionOptions> Options
    {
        get => (IHandlerOptions<AesGcmEncryptionOptions>)base.Options;
        set => base.Options = value;
    }

    /// <summary>
    /// Processes the input stream by decrypting it using AES-GCM and writing the plaintext data to the output stream.
    /// Reads the nonce first, then decrypts each chunk while verifying authentication tags.
    /// </summary>
    /// <param name="input">The input stream containing encrypted data to decrypt.</param>
    /// <param name="output">The output stream to write the decrypted plaintext data to.</param>
    /// <returns>A task representing the asynchronous decryption operation.</returns>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        ArgumentNullException.ThrowIfNull(Options.Value, nameof(Options.Value));

        var opts = Options.Value;

        if (opts.Key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes for AES-256-GCM");

        // Read nonce (first 12 bytes)
        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        
        int bytesRead = await input.ReadAsync(nonce.ToArray());
        if (bytesRead != nonce.Length)
            throw new InvalidDataException("Nonce too short");

        using var aesGcm = new AesGcm(opts.Key, opts.TagSizeInBytes);

        byte[] ciphertextBuffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
        byte[] plaintextBuffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
        byte[] tagBuffer = new byte[opts.TagSizeInBytes];

        try
        {
            int bytesReadCipher;
            while ((bytesReadCipher = await input.ReadAsync(ciphertextBuffer.AsMemory(0, ChunkSize))) > 0)
            {
                int bytesReadTag = await input.ReadAsync(tagBuffer.ToArray().AsMemory(0, opts.TagSizeInBytes));
                if (bytesReadTag != opts.TagSizeInBytes)
                    throw new InvalidDataException("Tag too short");

                // Decrypt the chunk
                aesGcm.Decrypt(
                    nonce.AsSpan(),
                    ciphertextBuffer.AsSpan(0, bytesReadCipher),
                    tagBuffer.AsSpan(),
                    plaintextBuffer.AsSpan(0, bytesReadCipher),
                    opts.Aad);

                await output.WriteAsync(plaintextBuffer.AsMemory(0, bytesReadCipher));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(ciphertextBuffer);
            ArrayPool<byte>.Shared.Return(plaintextBuffer);
            CryptographicOperations.ZeroMemory(nonce);
        }
    }
}