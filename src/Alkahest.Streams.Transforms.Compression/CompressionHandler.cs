namespace Alkahest.Streams.Transforms.Compression;

/// <summary>
/// Base class for compression handlers.
/// </summary>
public abstract class CompressionHandler : ITransformHandler
{
    /// <inheritdoc/>
    public abstract string Name { get; init; }
    /// <inheritdoc/>
    public IHandlerOptions Options { get; set; } = HandlerOptions.Empty;
    /// <inheritdoc/>
    public abstract Task ProcessAsync(Stream input, Stream output);
}
