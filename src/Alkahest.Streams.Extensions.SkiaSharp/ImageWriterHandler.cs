namespace Alkahest.Streams.Extensions.SkiaSharp;

/// <summary>
/// Abstract base class for image writing handlers that implement <see cref="ITransformHandler"/>.
/// Derived classes should process input streams and write image data to output streams.
/// </summary>
public abstract class ImageWriterHandler : ITransformHandler
{
    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    public virtual string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the options for the handler.
    /// </summary>
    public IHandlerOptions Options { get; set; } = HandlerOptions.Empty;

    /// <summary>
    /// Processes the input stream asynchronously and writes image data to the output stream.
    /// </summary>
    /// <param name="input">The input stream containing data to be transformed.</param>
    /// <param name="output">The output stream to write the transformed image data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task ProcessAsync(Stream input, Stream output);
}
