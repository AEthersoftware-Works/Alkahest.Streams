namespace Alkahest.Streams.Transforms.Encryption;

/// <summary>
/// Base class for decryption handlers that implement stream transformation.
/// </summary>
public abstract class DecryptionHandler : ITransformHandler
{
    /// <summary>
    /// Gets the name of the decryption handler.
    /// </summary>
    public virtual string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the options for the decryption handler.
    /// </summary>
    public IHandlerOptions Options { get; set; } = HandlerOptions.Empty;

    /// <summary>
    /// Asynchronously decrypts the input stream to the output stream.
    /// </summary>
    /// <param name="input">The input stream to decrypt.</param>
    /// <param name="output">The output stream for decrypted data.</param>
    public abstract Task ProcessAsync(Stream input, Stream output);
}