namespace Alkahest.Streams.Events;

using Performance;

/// <summary>
/// Provides data for the handler end event.
/// </summary>
public class HandlerEndEventArgs(
    string handlerName,
    int handlerIndex,
    TimeSpan duration,
    long bytesProcessed,
    Exception? lastException = null,
    TransformStats? statsBefore = null,
    TransformStats? statsAfter = null,
    TransformStatsDelta? statsDelta = null
) : HandlerEventArgs(handlerName, handlerIndex)
{
    /// <summary>
    /// Gets the duration of the handler processing.
    /// </summary>
    public TimeSpan Duration { get; init; } = duration;

    /// <summary>
    /// Gets the number of bytes processed by the handler.
    /// </summary>
    public long BytesProcessed { get; init; } = bytesProcessed; // Bytes read from input or written to output

    /// <summary>
    /// Gets a value indicating whether stats were tracked &amp; provided.
    /// </summary>
    public bool HasStats => Stats is not null;

    /// <summary>
    /// The handler performance stats.
    /// </summary>
    /// TODO: Change this argument in the constructor to simplify.
    public HandlerStatsSummary? Stats { get; init; } = new HandlerStatsSummary(statsBefore, statsAfter, statsDelta);

    /// <summary>
    /// Gets a value indicating whether the handler processing was successful.
    /// </summary>
    public bool Success => LastException is null;

    /// <summary>
    /// Gets the last exception that occurred during handler processing, if any.
    /// </summary>
    public Exception? LastException { get; init; } = lastException;
}