namespace Alkahest.Streams.Events;

using Performance;

/// <summary>
/// Provides data for the transform end event.
/// </summary>
public class TransformEndEventArgs(
    TimeSpan totalDuration,
    long totalBytesIn,
    long totalBytesOut,
    Exception? lastException = null,
    TransformStats? stats = null,
    HandlerStatsAggregate? handlerStatsAggregate = null,
    HandlerStatsDeltaCollection? statsDeltas = null) : TransformEventArgs
{
    /// <summary>
    /// Gets the timestamp when the transform ended.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the total duration of the transform process.
    /// </summary>
    public TimeSpan TotalDuration { get; init; } = totalDuration;

    /// <summary>
    /// Gets the total number of bytes read from the input.
    /// </summary>
    public long TotalBytesIn { get; init; } = totalBytesIn;

    /// <summary>
    /// Gets the total number of bytes written to the output.
    /// </summary>
    public long TotalBytesOut { get; init; } = totalBytesOut;

    /// <summary>
    /// Gets a value indicating whether the transform process was successful.
    /// </summary>
    public bool Success => LastException is null;

    /// <summary>
    /// Gets the last exception that occurred during the transform process, if any.
    /// </summary>
    public Exception? LastException { get; init; } = lastException;

    /// <summary>
    /// Gets a value indicating whether stats were tracked &amp; provided.
    /// </summary>
    public bool HasStats => Stats is not null;

    /// <summary>
    /// The handler performance stats.
    /// </summary>
    /// TODO: Change this argument in the constructor to simplify.
    public TransformStatsSummary? Stats { get; init; } = new TransformStatsSummary(stats, handlerStatsAggregate, statsDeltas);
}
