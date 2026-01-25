namespace Alkahest.Streams.Transforms.Encryption;

/// <summary>
/// Base class for decryption handlers.
/// </summary>
public abstract class DecryptionHandler : ITransformHandler
{
    public abstract string Name { get; init; }
    public IHandlerOptions Options { get; set; } = HandlerOptions.Empty;
    public abstract Task ProcessAsync(Stream input, Stream output);
}
