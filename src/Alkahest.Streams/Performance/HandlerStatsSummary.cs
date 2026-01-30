namespace Alkahest.Streams.Performance;

/// <summary>
/// Handler performance statistics.  See <see cref="Events.HandlerEndEventArgs"/> 
/// </summary>
/// <param name="statsBefore">Performance statistics captured before handler processing.</param>
/// <param name="statsAfter">Performance statistics captured after handler processing.</param>
/// <param name="statsDelta">The performance statistics delta.</param>
public class HandlerStatsSummary(TransformStats? statsBefore, TransformStats? statsAfter, TransformStatsDelta? statsDelta)
{
    /// <summary>
    /// Gets the performance statistics before handler processing.
    /// </summary>
    public TransformStats? StatsBefore { get; init; } = statsBefore;

    /// <summary>
    /// Gets the performance statistics after handler processing.
    /// </summary>
    public TransformStats? StatsAfter { get; init; } = statsAfter;

    /// <summary>
    /// Gets the delta of performance statistics during handler processing.
    /// </summary>
    public TransformStatsDelta? StatsDelta { get; init; } = statsDelta;

}