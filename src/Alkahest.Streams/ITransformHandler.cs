namespace Alkahest.Streams;

/// <summary>
/// Defines a handler for stream transformations.
/// </summary>
public interface ITransformHandler
{
    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    string Name { get; init; }

    /// <summary>
    /// Gets or sets the options for the handler.
    /// </summary>
    IHandlerOptions Options { get; set; }

    /// <summary>
    /// Processes the input stream and writes the result to the output stream.
    /// </summary>
    /// <param name="input">The input stream to process.</param>
    /// <param name="output">The output stream to write to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(Stream input, Stream output);
}
