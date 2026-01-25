namespace Alkahest.Streams;

using System.Diagnostics;
using Alkahest.Streams.Events;

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
    void AddStreamHandler<THandler,THandlerOptions>(THandlerOptions options) 
        where THandler : ITransformHandler, new()
        where THandlerOptions : class;

    /// <summary>
    /// Adds a handler and returns the builder for chaining.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder ProcessWith<THandler>() where THandler : ITransformHandler, new();

    /// <summary>
    /// Sets the processing strategy for the transformer.
    /// </summary>
    /// <param name="strategy">The strategy to use.</param>
    /// <returns>The builder instance for chaining.</returns>
    ITransformBuilder UseStrategy(HandlerStrategy strategy);

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

/// <summary>
/// Builder class for creating stream transformers.
/// </summary>
public class TransformBuilder : ITransformBuilder
{
    private const HandlerStrategy DefaultStrategy = HandlerStrategy.Auto;
    private HandlerStrategy _strategy = DefaultStrategy;
    private readonly List<ITransformHandler> _handlers = [];
    private readonly List<EventHandler<TransformBeginEventArgs>> _transformBeginEventBindings = [];
    private readonly List<EventHandler<HandlerBeginEventArgs>> _handlerBeginEventBindings = [];
    private readonly List<EventHandler<HandlerEndEventArgs>> _handlerEndEventBindings = [];
    private readonly List<EventHandler<TransformEndEventArgs>> _transformEndEventBindings = [];

    /// <summary>
    /// Adds a new handler of the specified type to the transformation chain.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    public void AddStreamHandler<THandler>() where THandler : ITransformHandler, new()
    {
        var handler = new THandler();
        _handlers.Add(handler);
    }

    /// <summary>
    /// Adds a new handler of the specified type with options to the transformation chain.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    /// <typeparam name="THandlerOptions">The type of options for the handler.</typeparam>
    /// <param name="options">The options to configure the handler.</param>
    public void AddStreamHandler<THandler, THandlerOptions>(THandlerOptions options)
        where THandler : ITransformHandler, new()
        where THandlerOptions : class
    {
        var handler = new THandler { Options = HandlerOptions.Create(options) };
        _handlers.Add(handler);
    }

    /// <summary>
    /// Adds a handler and returns the builder for chaining.
    /// </summary>
    /// <typeparam name="THandler">The type of handler to add.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public ITransformBuilder ProcessWith<THandler>() where THandler : ITransformHandler, new()
    {
        AddStreamHandler<THandler>();
        return this;
    }

    /// <summary>
    /// Sets the processing strategy for the transformer.
    /// </summary>
    /// <param name="strategy">The strategy to use.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ITransformBuilder UseStrategy(HandlerStrategy strategy)
    {
        _strategy = strategy;
        return this;
    }

    /// <summary>
    /// Adds a telemetry event handler for transform end events.
    /// </summary>
    /// <param name="action">The event handler to add.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ITransformBuilder UseTelemetry(EventHandler<TransformEndEventArgs> action)
    {
        _transformEndEventBindings.Add(action);
        return this;
    }

    /// <summary>
    /// Applies the configured event bindings to the transformer.
    /// </summary>
    /// <param name="transformer">The transformer to apply bindings to.</param>
    protected void ApplyEventBindings(ITransformer transformer)
    {
        // Apply event bindings
        foreach (var transformBeginEvent in _transformBeginEventBindings)
            transformer.TransformProcessBegin += transformBeginEvent;

        foreach (var handlerBeginEvent in _handlerBeginEventBindings)
            transformer.HandlerProcessBegin += handlerBeginEvent;

        foreach (var handlerEndEvent in _handlerEndEventBindings)
            transformer.HandlerProcessEnd += handlerEndEvent;

        foreach (var transformEndEvent in _transformEndEventBindings)
            transformer.TransformProcessEnd += transformEndEvent;
    }

    /// <summary>
    /// Builds the transformer with the configured handlers and strategy.
    /// </summary>
    /// <typeparam name="TStreamTransformer">The type of transformer to build.</typeparam>
    /// <returns>The built transformer.</returns>
    public TStreamTransformer Build<TStreamTransformer>() where TStreamTransformer : Transformer, ITransformer, new()
    {
        var transformer = _strategy == DefaultStrategy
            ? TStreamTransformer.WithHandlers<TStreamTransformer>(_handlers)
            : TStreamTransformer.WithHandlersAndStrategy<TStreamTransformer>(_handlers, _strategy);

        ApplyEventBindings(transformer);

        return transformer;
    }
    /// <summary>
    /// Builds a default stream transformer with the configured handlers and strategy.
    /// </summary>
    /// <returns>The built default stream transformer.</returns>
    public DefaultStreamTransformer Build() => Build<DefaultStreamTransformer>();
}
