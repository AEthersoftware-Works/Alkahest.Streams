namespace Alkahest.Streams;

using Events;

/// <summary>
/// Builder class for creating stream transformers.
/// </summary>
public class TransformBuilder : ITransformBuilder
{
    /// <summary>
    /// The default handler strategy used by the builder.
    /// </summary>
    protected const HandlerStrategy DefaultStrategy = HandlerStrategy.Auto;

    /// <summary>
    /// The default threshold that the transformer will use the Buffered strategy.
    /// </summary>
    protected const long BufferedStreamThresholdDefault = 26_214_400; // 25 MB

    /// <summary>
    /// The maximum safe length for the Buffered strategy to prevent memory issues.
    /// </summary>
    protected const long BufferedStreamMaxLength = 1_610_612_736; // 1.5 GB

    /// <summary>
    /// The current handler strategy configured for the transformer.
    /// </summary>
    protected HandlerStrategy Strategy { get; set; } = DefaultStrategy;

    /// <summary>
    /// The current threshold for switching to the Buffered strategy when using Auto mode.
    /// </summary>
    protected long BufferedStreamThreshold
    {
        get;
        set
        {
            BufferedStrategyNotSuitableException.ThrowIfStreamLengthGreater(value, BufferedStreamMaxLength);
            field = value;
        }
    } = BufferedStreamThresholdDefault;

    /// <summary>
    /// The list of transform handlers to be applied in sequence.
    /// </summary>
    protected List<ITransformHandler> Handlers { get; } = [];

    /// <summary>
    /// Event bindings for transform begin events.
    /// </summary>
    protected List<EventHandler<TransformBeginEventArgs>> TransformBeginEventBindings { get; } = [];

    /// <summary>
    /// Event bindings for handler begin events.
    /// </summary>
    protected List<EventHandler<HandlerBeginEventArgs>> HandlerBeginEventBindings { get; } = [];

    /// <summary>
    /// Event bindings for handler end events.
    /// </summary>
    protected List<EventHandler<HandlerEndEventArgs>> HandlerEndEventBindings { get; } = [];

    /// <summary>
    /// Event bindings for transform end events.
    /// </summary>
    protected List<EventHandler<TransformEndEventArgs>> TransformEndEventBindings { get; } = [];

    /// <inheritdoc/>
    public void AddStreamHandler<THandler>() where THandler : ITransformHandler, new()
    {
        var handler = new THandler();
        Handlers.Add(handler);
    }

    /// <inheritdoc/>
    public void AddStreamHandler<THandler, THandlerOptions>(THandlerOptions options)
        where THandler : ITransformHandler, new()
        where THandlerOptions : new()
    {
        var handler = new THandler { Options = HandlerOptions.Create(options) };
        Handlers.Add(handler);
    }

    /// <inheritdoc/>
    public void AddStreamHandler<THandler, THandlerOptions>(Action<THandlerOptions>? configure = null)
        where THandler : ITransformHandler, new()
        where THandlerOptions : class, new()
    {
        var options = new THandlerOptions();
        configure?.Invoke(options);

        AddStreamHandler<THandler, THandlerOptions>(options);
    }

    /// <inheritdoc/>
    public ITransformBuilder ProcessWith<THandler>() where THandler : ITransformHandler, new()
    {
        AddStreamHandler<THandler>();
        return this;
    }

    /// <inheritdoc/>
    public ITransformBuilder ProcessWith<THandler, THandlerOptions>(THandlerOptions options)
        where THandler : ITransformHandler, new()
        where THandlerOptions : new()
    {
        AddStreamHandler<THandler, THandlerOptions>(options);
        return this;
    }

    /// <inheritdoc/>
    public ITransformBuilder ProcessWith<THandler, THandlerOptions>(Action<THandlerOptions>? configure = null)
        where THandler : ITransformHandler, new()
        where THandlerOptions : class, new()
    {
        AddStreamHandler<THandler, THandlerOptions>(configure);
        return this;
    }

    /// <inheritdoc/>
    public ITransformBuilder UseStrategy(HandlerStrategy strategy)
    {
        Strategy = strategy;
        return this;
    }

    /// <inheritdoc/>
    public ITransformBuilder UseAutoStrategyWithThreshold(long bufferedPoolThreshold = 26214400)
    {
        Strategy = HandlerStrategy.Auto;
        BufferedStreamThreshold = bufferedPoolThreshold;
        return this;
    }

    // TODO: This is incredibly basic usage, it is optimal for telemetry to subscribe
    //       to the metrics channel for non-blocking statistics data capture.

    /// <inheritdoc/>
    public ITransformBuilder UseTelemetry(EventHandler<TransformEndEventArgs> action)
    {
        TransformEndEventBindings.Add(action);
        return this;
    }

    /// <inheritdoc/>
    public ITransformBuilder AddTransformBeginEventListener(EventHandler<TransformBeginEventArgs> listener)
    {
        TransformBeginEventBindings.Add(listener);
        return this;
    }

    /// <inheritdoc/>
    public ITransformBuilder AddHandlerBeginEventListener(EventHandler<HandlerBeginEventArgs> listener)
    {
        HandlerBeginEventBindings.Add(listener);
        return this;
    }

    /// <inheritdoc/>
    public ITransformBuilder AddHandlerEndEventListener(EventHandler<HandlerEndEventArgs> listener)
    {
        HandlerEndEventBindings.Add(listener);
        return this;
    }

    /// <inheritdoc/>
    public ITransformBuilder AddTransformEndEventListener(EventHandler<TransformEndEventArgs> listener)
    {
        TransformEndEventBindings.Add(listener);
        return this;
    }

    /// <summary>
    /// Applies the configured event bindings to the transformer.
    /// </summary>
    /// <param name="transformer">The transformer to apply bindings to.</param>
    protected void ApplyEventBindings(ITransformer transformer)
    {
        // Apply event bindings
        foreach (var transformBeginEventListener in TransformBeginEventBindings)
            transformer.TransformProcessBegin += transformBeginEventListener;

        foreach (var handlerBeginEventListener in HandlerBeginEventBindings)
            transformer.HandlerProcessBegin += handlerBeginEventListener;

        foreach (var handlerEndEventListener in HandlerEndEventBindings)
            transformer.HandlerProcessEnd += handlerEndEventListener;

        foreach (var transformEndEventListener in TransformEndEventBindings)
            transformer.TransformProcessEnd += transformEndEventListener;
    }

    /// <inheritdoc/>
    public TStreamTransformer Build<TStreamTransformer>() where TStreamTransformer : Transformer, ITransformer, new()
    {
        var transformer = TStreamTransformer.Create<TStreamTransformer>(
            handlers: Handlers,
            strategy: Strategy,
            bufferedPoolThreshold: BufferedStreamThreshold
        );

        ApplyEventBindings(transformer);

        return transformer;
    }

    /// <summary>
    /// Builds a default stream transformer with the configured handlers and strategy.
    /// </summary>
    /// <returns>The built default stream transformer.</returns>
    public DefaultStreamTransformer Build() => Build<DefaultStreamTransformer>();
}
