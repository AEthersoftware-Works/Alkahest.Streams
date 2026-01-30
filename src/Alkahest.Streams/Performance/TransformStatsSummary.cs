namespace Alkahest.Streams.Performance;

/// <summary>
/// Represents a comprehensive summary of performance statistics for a transform operation,
/// including the final transform stats, aggregated handler stats, and per-handler deltas.
/// </summary>
/// <param name="transformStats">The final performance statistics of the transform process.</param>
/// <param name="handlerStatsAggregate">The computed overall performance statistics for all handlers.</param>
/// <param name="statsDeltas">The performance statistics deltas captured for each handler.</param>
public class TransformStatsSummary(TransformStats? transformStats, HandlerStatsAggregate? handlerStatsAggregate, List<TransformStatsDelta>? statsDeltas)
{

    /// <summary>
    /// Gets the final performance statistics of the transform process.
    /// </summary>
    public TransformStats? Stats { get; init; } = transformStats;

    /// <summary>
    /// Gets the computed overall performance statistics for handlers.
    /// </summary>
    public HandlerStatsAggregate? HandlerStatsAggregate { get; init; } = handlerStatsAggregate;

    /// <summary>
    /// Gets the performance statistics captured for each handler.
    /// </summary>
    public List<TransformStatsDelta>? HandlerStatsDeltas { get; init; } = statsDeltas;
}
