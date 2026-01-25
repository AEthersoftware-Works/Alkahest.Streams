namespace Alkahest.Streams;

using System.Threading.Channels;

using Events;

/// <summary>
/// Defines a transformer that processes streams through a chain of handlers.
/// </summary>
public interface ITransformer
{
    /// <summary>
    /// Occurs when a transform process begins.
    /// </summary>
    event EventHandler<TransformBeginEventArgs>? TransformProcessBegin;

    /// <summary>
    /// Occurs when a handler process begins.
    /// </summary>
    event EventHandler<HandlerBeginEventArgs>? HandlerProcessBegin;

    /// <summary>
    /// Occurs when a handler process ends.
    /// </summary>
    event EventHandler<HandlerEndEventArgs>? HandlerProcessEnd;

    /// <summary>
    /// Occurs when a transform process ends.
    /// </summary>
    event EventHandler<TransformEndEventArgs>? TransformProcessEnd;

    /// <summary>
    /// Creates a transformer instance with the specified handlers.
    /// </summary>
    /// <typeparam name="TTransformer">The type of the transformer.</typeparam>
    /// <param name="handlers">The list of transform handlers.</param>
    /// <returns>A new transformer instance.</returns>
    static abstract TTransformer WithHandlers<TTransformer>(List<ITransformHandler> handlers) where TTransformer : Transformer, ITransformer, new();

    /// <summary>
    /// Creates a transformer instance with the specified handlers and strategy.
    /// </summary>
    /// <typeparam name="TTransformer">The type of the transformer.</typeparam>
    /// <param name="handlers">The list of transform handlers.</param>
    /// <param name="strategy">The handler strategy to use.</param>
    /// <returns>A new transformer instance.</returns>
    static abstract TTransformer WithHandlersAndStrategy<TTransformer>(List<ITransformHandler> handlers, HandlerStrategy strategy) where TTransformer : Transformer, ITransformer, new();

    /// <summary>
    /// Processes the input stream asynchronously and writes the result to the output stream.
    /// </summary>
    /// <param name="input">The input stream to process.</param>
    /// <param name="output">The output stream to write the transformed data to.</param>
    /// <param name="bufferSize">The buffer size to use for processing. Default is 65536 bytes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessAsync(Stream input, Stream output, int bufferSize = 65536);

    /// <summary>
    /// Reader for high-throughput, non-blocking metrics consumption.
    /// </summary>
    ChannelReader<TransformEventArgs> MetricsChannelReader { get; }
}

