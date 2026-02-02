namespace Alkahest.Streams.Extensions.SkiaSharp;

/// <summary>
/// Abstract base class for image reading handlers that implement <see cref="ITransformHandler"/>.
/// Derived classes should read image data from input streams and transform it for output.
/// </summary>
public abstract class ImageReaderHandler : ITransformHandler
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
    /// Processes the input stream asynchronously, reading image data and writing transformed data to the output stream.
    /// </summary>
    /// <param name="input">The input stream containing image data to be read.</param>
    /// <param name="output">The output stream to write the transformed data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task ProcessAsync(Stream input, Stream output);
}
