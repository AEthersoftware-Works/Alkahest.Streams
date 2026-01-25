namespace Alkahest.Streams.Transforms.Encryption;

/// <summary>
/// Base class for encryption handlers.
/// </summary>
public abstract class EncryptionHandler : ITransformHandler
{
    public abstract string Name { get; init; }
    public IHandlerOptions Options { get; set; } = HandlerOptions.Empty;
    public abstract Task ProcessAsync(Stream input, Stream output);
}

