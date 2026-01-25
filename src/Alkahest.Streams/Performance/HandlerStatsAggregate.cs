namespace Alkahest.Streams.Performance;

/// <summary>
/// Represents aggregated performance statistics for a transform handler.
/// This class provides various metrics such as memory usage, processing time, bytes processed, and garbage collection activity.
/// All delta values represent changes per handler invocation.
/// </summary>
public sealed class HandlerStatsAggregate
{
    /// <summary>
    /// Gets the total number of handler invocations aggregated in this instance.
    /// </summary>
    public long TotalInvocations { get; init; }

    /// <summary>
    /// Gets the average change in peak working set bytes per handler invocation.
    /// </summary>
    public double AvgDeltaPeakWorkingSetBytes { get; init; }

    /// <summary>
    /// Gets the minimum change in peak working set bytes across all handler invocations.
    /// </summary>
    public long MinDeltaPeakWorkingSetBytes { get; init; }

    /// <summary>
    /// Gets the maximum change in peak working set bytes across all handler invocations.
    /// </summary>
    public long MaxDeltaPeakWorkingSetBytes { get; init; }

    /// <summary>
    /// Gets the average change in private memory bytes per handler invocation.
    /// </summary>
    public double AvgDeltaPrivateMemoryBytes { get; init; }

    /// <summary>
    /// Gets the minimum change in private memory bytes across all handler invocations.
    /// </summary>
    public long MinDeltaPrivateMemoryBytes { get; init; }

    /// <summary>
    /// Gets the maximum change in private memory bytes across all handler invocations.
    /// </summary>
    public long MaxDeltaPrivateMemoryBytes { get; init; }

    /// <summary>
    /// Gets the average change in processor time (in milliseconds) per handler invocation.
    /// </summary>
    public double AvgDeltaProcessorTimeMs { get; init; }

    /// <summary>
    /// Gets the minimum change in processor time (in milliseconds) across all handler invocations.
    /// </summary>
    public double MinDeltaProcessorTimeMs { get; init; }

    /// <summary>
    /// Gets the maximum change in processor time (in milliseconds) across all handler invocations.
    /// </summary>
    public double MaxDeltaProcessorTimeMs { get; init; }

    /// <summary>
    /// Gets the average change in total processing time (in milliseconds) per handler invocation.
    /// </summary>
    public double AvgDeltaTimeMs { get; init; }

    /// <summary>
    /// Gets the minimum change in total processing time (in milliseconds) across all handler invocations.
    /// </summary>
    public double MinDeltaTimeMs { get; init; }

    /// <summary>
    /// Gets the maximum change in total processing time (in milliseconds) across all handler invocations.
    /// </summary>
    public double MaxDeltaTimeMs { get; init; }

    /// <summary>
    /// Gets the average number of bytes read (input) per handler invocation.
    /// </summary>
    public double AvgDeltaBytesIn { get; init; }

    /// <summary>
    /// Gets the minimum number of bytes read (input) across all handler invocations.
    /// </summary>
    public long MinDeltaBytesIn { get; init; }

    /// <summary>
    /// Gets the maximum number of bytes read (input) across all handler invocations.
    /// </summary>
    public long MaxDeltaBytesIn { get; init; }

    /// <summary>
    /// Gets the average number of bytes written (output) per handler invocation.
    /// </summary>
    public double AvgDeltaBytesOut { get; init; }

    /// <summary>
    /// Gets the minimum number of bytes written (output) across all handler invocations.
    /// </summary>
    public long MinDeltaBytesOut { get; init; }

    /// <summary>
    /// Gets the maximum number of bytes written (output) across all handler invocations.
    /// </summary>
    public long MaxDeltaBytesOut { get; init; }

    /// <summary>
    /// Gets the average change in garbage collection count for generation 0 per handler invocation.
    /// </summary>
    public double AvgDeltaGCCountGen0 { get; init; }

    /// <summary>
    /// Gets the average change in garbage collection count for generation 1 per handler invocation.
    /// </summary>
    public double AvgDeltaGCCountGen1 { get; init; }

    /// <summary>
    /// Gets the average change in garbage collection count for generation 2 per handler invocation.
    /// </summary>
    public double AvgDeltaGCCountGen2 { get; init; }

    /// <summary>
    /// Gets a value indicating whether the average memory impact is considered high (>50 MB per invocation).
    /// </summary>
    public bool HasHighAvgMemoryImpact => AvgDeltaPeakWorkingSetBytes > 50 * 1024 * 1024; // >50 MB avg per handler

    /// <summary>
    /// Gets a value indicating whether the average processing time impact is considered high (>100 ms per invocation).
    /// </summary>
    public bool HasHighAvgTimeImpact => AvgDeltaTimeMs > 100;

    /// <summary>
    /// Gets a value indicating whether the average CPU time impact is considered high (>50 ms per invocation).
    /// </summary>
    public bool HasHighAvgCpuImpact => AvgDeltaProcessorTimeMs > 50;
}