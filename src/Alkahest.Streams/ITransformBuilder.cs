using Alkahest.Streams.Events;

namespace Alkahest.Streams;

/// <summary>
/// Interface for building stream transformers with handlers and strategies.
/// </summary>
public interface ITransformBuilder
{
    /// <summary>
    /// Adds a new handler of the specified type to the transformation chain.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    void AddStreamHandler<THandler>() where THandler : ITransformHandler, new();

    /// <summary>
    /// Adds a new handler of the specified type with configuration options to 
    /// the transformation chain.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    /// <typeparam name="THandlerOptions">The type of handler configuration options.</typeparam>
    /// <param name="options">The instance of handler configuration options.</param>
    void AddStreamHandler<THandler, THandlerOptions>(THandlerOptions options)
        where THandler : ITransformHandler, new()
        where THandlerOptions : new();


    /// <summary>
    /// Adds a new handler of the specified type with options configured by an action to the transformation chain.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    /// <typeparam name="THandlerOptions">The type of handler configuration options.</typeparam>
    /// <param name="configure">An optional delegate that configures the <typeparamref name="THandlerOptions"/> instance before it's used by the handler. This allows customization of settings in a fluent manner similar to dependency injection configuration patterns.</param>
    void AddStreamHandler<THandler, THandlerOptions>(Action<THandlerOptions>? configure = null)
        where THandler : ITransformHandler, new()
        where THandlerOptions : class, new();

    /// <summary>
    /// Adds a handler and returns the builder for chaining.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder ProcessWith<THandler>() where THandler : ITransformHandler, new();

    /// <summary>
    /// Adds a new handler of the specified type with configuration options to the transformation chain.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    /// <typeparam name="THandlerOptions">The type of handler configuration options.</typeparam>
    /// <param name="options">The instance of handler configuration options.</param>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder ProcessWith<THandler, THandlerOptions>(THandlerOptions options)
        where THandler : ITransformHandler, new()
        where THandlerOptions : new();

    /// <summary>
    /// Adds a new handler of the specified type with options configured by an action to the transformation chain.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    /// <typeparam name="THandlerOptions">The type of handler configuration options.</typeparam>
    /// <param name="configure">An optional delegate that configures the <typeparamref name="THandlerOptions"/> instance before it's used by the handler. This allows customization of settings in a fluent manner similar to dependency injection configuration patterns.</param>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder ProcessWith<THandler, THandlerOptions>(Action<THandlerOptions>? configure = null)
        where THandler : ITransformHandler, new()
        where THandlerOptions : class, new();

    /// <summary>
    /// Adds an event listener for the transform begin event.
    /// </summary>
    /// <param name="handler">The event handler to add.</param>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder AddTransformBeginEventListener(EventHandler<TransformBeginEventArgs> handler);

    /// <summary>
    /// Adds an event listener for the handler begin event.
    /// </summary>
    /// <param name="handler">The event handler to add.</param>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder AddHandlerBeginEventListener(EventHandler<HandlerBeginEventArgs> handler);

    /// <summary>
    /// Adds an event listener for the handler end event.
    /// </summary>
    /// <param name="handler">The event handler to add.</param>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder AddHandlerEndEventListener(EventHandler<HandlerEndEventArgs> handler);

    /// <summary>
    /// Adds an event listener for the transform end event.
    /// </summary>
    /// <param name="handler">The event handler to add.</param>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder AddTransformEndEventListener(EventHandler<TransformEndEventArgs> handler);

    /// <summary>
    /// Sets the processing strategy for the transformer.
    /// </summary>
    /// <param name="strategy">The strategy to use.</param>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder UseStrategy(HandlerStrategy strategy);

    /// <summary>
    /// Sets the processing strategy for the transformer to Auto, allowing fluent configuration of the threshold
    /// for switching to the Buffered strategy. The Buffered strategy uses <see cref="PooledMemoryStream"/> for
    /// efficient memory management on smaller streams.
    /// </summary>
    /// <param name="bufferedPoolThreshold">The maximum threshold in bytes (default 25 MB) that will use the Buffered strategy.
    /// Must not exceed 1.5 GB to prevent excessive memory usage and/or <see cref="OutOfMemoryException"/> exceptions.</param>
    /// <returns>The builder instance for chaining.</returns>
    /// <exception cref="BufferedStrategyNotSuitableException">Thrown if <paramref name="bufferedPoolThreshold"/> exceeds 1.5 GB.</exception>
    ITransformBuilder UseAutoStrategyWithThreshold(long bufferedPoolThreshold = 26_214_400);

    /// <summary>
    /// Builds the transformer with the configured handlers and strategy.
    /// </summary>
    /// <typeparam name="TStreamTransformer">The type of transformer to build.</typeparam>
    /// <returns>The built transformer.</returns>
    TStreamTransformer Build<TStreamTransformer>() where TStreamTransformer : Transformer, ITransformer, new();

    /// <summary>
    /// Builds a default stream transformer with the configured handlers and strategy.
    /// </summary>
    /// <returns>The built default stream transformer.</returns>
    DefaultStreamTransformer Build();
}
