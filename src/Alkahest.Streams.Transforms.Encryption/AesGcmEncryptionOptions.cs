using System.Buffers;
using System.Security.Cryptography;

namespace Alkahest.Streams.Transforms.Encryption;

/// <summary>
/// Represents the configuration options for encryption and decryption operations using AES-GCM.
/// </summary>
public sealed class AesGcmEncryptionOptions : IDisposable
{
    private byte[]? _key;
    private byte[]? _iv;
    private byte[]? _aad;

    /// <summary>
    /// Gets or sets the encryption key. Must be 32 bytes for AES-256.
    /// </summary>
    public ReadOnlySpan<byte> Key
    {
        get => _key;
        init
        {
            if (value.Length != 32)
                throw new ArgumentException("Key must be 32 bytes for AES-256-GCM");

            // Handles setting via Reflection.
            if (_key != null)
            {
                CryptographicOperations.ZeroMemory(_key.AsSpan(0, 32));
                ArrayPool<byte>.Shared.Return(_key, clearArray: true);
            }

            _key = ArrayPool<byte>.Shared.Rent(32);
            value.CopyTo(_key.AsSpan(0, 32));
        }
    }

    /// <summary>
    /// Gets or sets the additional authenticated data (AAD) for integrity verification.
    /// </summary>
    public ReadOnlySpan<byte> Aad
    {
        get => _aad ?? ReadOnlySpan<byte>.Empty;
        init
        {
            // Handles setting via Reflection.
            if (_aad != null)
            {
                CryptographicOperations.ZeroMemory(_aad.AsSpan(0, _aad.Length));
                ArrayPool<byte>.Shared.Return(_aad, clearArray: true);
            }

            _aad = ArrayPool<byte>.Shared.Rent(value.Length);
            value.CopyTo(_aad.AsSpan(0, value.Length));
        }
    }

    /// <summary>
    /// Gets or sets the size of the authentication tag in bytes. Default is 12 bytes (96 bits).
    /// </summary>
    public int TagSizeInBytes {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, AesGcm.NonceByteSizes.MinSize, nameof(TagSizeInBytes));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, AesGcm.NonceByteSizes.MaxSize, nameof(TagSizeInBytes));

            field = value;
        }
    } = AesGcm.NonceByteSizes.MaxSize;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_key != null)
        {
            CryptographicOperations.ZeroMemory(_key.AsSpan(0, 32));
            ArrayPool<byte>.Shared.Return(_key, clearArray: true);
            _key = null;
        }

        if (_iv != null)
        {
            CryptographicOperations.ZeroMemory(_iv.AsSpan(0, _iv.Length));
            _iv = null;
        }

        if (_aad != null)
        {
            CryptographicOperations.ZeroMemory(_aad.AsSpan(0, _aad.Length));
            ArrayPool<byte>.Shared.Return(_aad, clearArray: true);
            _aad = null;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizes the instance, ensuring that sensitive cryptographic data is securely cleared from memory.
    /// </summary>
    ~AesGcmEncryptionOptions()
    {
        Dispose();
    }
}
