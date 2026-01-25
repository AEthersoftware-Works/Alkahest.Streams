namespace Alkahest.Streams.Performance;

/// <summary>
/// Represents the delta of performance statistics between two points in time.
/// </summary>
public class TransformStatsDelta
{
    /// <summary>
    /// Gets the delta in peak working set bytes.
    /// </summary>
    public long DeltaPeakWorkingSetBytes { get; init; }

    /// <summary>
    /// Gets the delta in working set bytes.
    /// </summary>
    public long DeltaWorkingSetBytes { get; init; }

    /// <summary>
    /// Gets the delta in private memory bytes.
    /// </summary>
    public long DeltaPrivateMemoryBytes { get; init; }

    /// <summary>
    /// Gets the delta in GC allocated bytes.
    /// </summary>
    public long DeltaGCAllocatedBytes { get; init; }

    /// <summary>
    /// Gets the delta in GC total memory bytes.
    /// </summary>
    public long DeltaGCTotalMemoryBytes { get; init; }

    /// <summary>
    /// Gets the delta in Gen0 garbage collection count.
    /// </summary>
    public int DeltaGCCountGen0 { get; init; }

    /// <summary>
    /// Gets the delta in Gen1 garbage collection count.
    /// </summary>
    public int DeltaGCCountGen1 { get; init; }

    /// <summary>
    /// Gets the delta in Gen2 garbage collection count.
    /// </summary>
    public int DeltaGCCountGen2 { get; init; }

    /// <summary>
    /// Gets the delta in processor time.
    /// </summary>
    public TimeSpan DeltaProcessorTime { get; init; }

    /// <summary>
    /// Gets the delta in thread count.
    /// </summary>
    public int DeltaThreadCount { get; init; }

    /// <summary>
    /// Gets the delta in bytes read.
    /// </summary>
    public long DeltaBytesIn { get; init; }

    /// <summary>
    /// Gets the delta in bytes written.
    /// </summary>
    public long DeltaBytesOut { get; init; }

    /// <summary>
    /// Gets the duration of the measured interval.
    /// </summary>
    public TimeSpan Duration { get; init; } // Duration of the measured interval

    // Helper properties for alerts
    /// <summary>
    /// Gets a value indicating whether there is a significant memory increase.
    /// </summary>
    public bool HasSignificantMemoryIncrease => DeltaPeakWorkingSetBytes > 100 * 1024 * 1024; // > 100 MB increase

    /// <summary>
    /// Gets a value indicating whether there is high garbage collection activity.
    /// </summary>
    public bool HasHighGCActivity => DeltaGCCountGen2 > 10; // Tune threshold
}
