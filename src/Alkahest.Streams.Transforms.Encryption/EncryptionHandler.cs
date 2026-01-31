namespace Alkahest.Streams.Transforms.Encryption;

/// <summary>
/// Base class for encryption handlers that implement stream transformation.
/// </summary>
public abstract class EncryptionHandler : ITransformHandler
{
    /// <summary>
    /// Gets the name of the encryption handler.
    /// </summary>
    public virtual string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the options for the encryption handler.
    /// </summary>
    public IHandlerOptions Options { get; set; } = HandlerOptions.Empty;

    /// <summary>
    /// Asynchronously encrypts the input stream to the output stream.
    /// </summary>
    /// <param name="input">The input stream to encrypt.</param>
    /// <param name="output">The output stream for encrypted data.</param>
    public abstract Task ProcessAsync(Stream input, Stream output);
}