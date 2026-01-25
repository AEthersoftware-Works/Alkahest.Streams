namespace Alkahest.Streams.Performance;

/// <summary>
/// Represents performance statistics captured during stream transformation.
/// </summary>
public sealed class TransformStats
{
    // Memory metrics
    /// <summary>
    /// Gets the peak working set bytes used by the process.
    /// Max physical memory used by the process
    /// </summary>
    public long PeakWorkingSetBytes { get; init; }

    /// <summary>
    /// Gets the current working set bytes.
    /// Current physical memory
    /// </summary>
    public long WorkingSetBytes { get; init; }

    /// <summary>
    /// Gets the private memory bytes.
    /// Private bytes (most accurate for leaks)
    /// </summary>
    public long PrivateMemoryBytes { get; init; }

    /// <summary>
    /// Gets the total bytes allocated by the GC.
    /// </summary>
    public long GCAllocatedBytes { get; init; }

    /// <summary>
    /// Gets the total memory managed by the GC.
    /// </summary>
    public long GCTotalMemoryBytes { get; init; }

    // GC pressure indicators
    /// <summary>
    /// Gets the number of Gen0 garbage collections.
    /// </summary>
    public int GCCountGen0 { get; init; }

    /// <summary>
    /// Gets the number of Gen1 garbage collections.
    /// </summary>
    public int GCCountGen1 { get; init; }

    /// <summary>
    /// Gets the number of Gen2 garbage collections.
    /// </summary>
    public int GCCountGen2 { get; init; }

    // CPU & threading
    /// <summary>
    /// Gets the total processor time used by the process.
    /// </summary>
    public TimeSpan TotalProcessorTime { get; init; }

    /// <summary>
    /// Gets the number of threads in the process.
    /// </summary>
    public int ThreadCount { get; init; }

    // Optional: custom app-level counters (bytes processed, etc.)
    /// <summary>
    /// Gets the number of bytes read.
    /// </summary>
    public long BytesIn { get; init; }

    /// <summary>
    /// Gets the number of bytes written.
    /// </summary>
    public long BytesOut { get; init; }

    /// <summary>
    /// Gets the compression ratio.
    /// </summary>
    public double CompressionRatio => BytesIn > 0 ? (double)BytesIn / BytesOut : 0;

    // Timestamp & duration
    /// <summary>
    /// Gets the timestamp when the stats were captured.
    /// </summary>
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the duration of the operation.
    /// </summary>
    public TimeSpan Duration { get; init; }

    // Helper to detect anomalies (can be used for alerts)
    /// <summary>
    /// Gets a value indicating whether there is high memory pressure.
    /// </summary>
    public bool HasHighMemoryPressure => PeakWorkingSetBytes > 2L * 1024 * 1024 * 1024; // > 2 GB

    // arbitrary threshold, tune as needed
    /// <summary>
    /// Gets a value indicating whether there is excessive garbage collection.
    /// </summary>
    public bool HasExcessiveGC => GCCountGen2 > 50;

    // heuristic
    /// <summary>
    /// Gets a value indicating whether a memory leak is suspected.
    /// </summary>
    public bool IsMemoryLeakSuspect => PrivateMemoryBytes > PeakWorkingSetBytes * 1.5;

    /// <summary>
    /// Computes the difference between this stats instance and a previous one.
    /// </summary>
    /// <param name="before">The previous stats instance.</param>
    /// <returns>The difference in stats.</returns>
    public TransformStatsDelta Diff(
            TransformStats before)
    {
        return new TransformStatsDelta
        {
            DeltaPeakWorkingSetBytes = PeakWorkingSetBytes - before.PeakWorkingSetBytes,
            DeltaWorkingSetBytes = WorkingSetBytes - before.WorkingSetBytes,
            DeltaPrivateMemoryBytes = PrivateMemoryBytes - before.PrivateMemoryBytes,
            DeltaGCAllocatedBytes = GCAllocatedBytes - before.GCAllocatedBytes,
            DeltaGCTotalMemoryBytes = GCTotalMemoryBytes - before.GCTotalMemoryBytes,

            DeltaGCCountGen0 = GCCountGen0 - before.GCCountGen0,
            DeltaGCCountGen1 = GCCountGen1 - before.GCCountGen1,
            DeltaGCCountGen2 = GCCountGen2 - before.GCCountGen2,

            DeltaProcessorTime = TotalProcessorTime - before.TotalProcessorTime,
            DeltaThreadCount = ThreadCount - before.ThreadCount,

            DeltaBytesIn = BytesIn - before.BytesIn,
            DeltaBytesOut = BytesOut - before.BytesOut,

            Duration = Duration - before.Duration
        };
    }

    /// <summary>
    /// Computes the difference between two stats instances.
    /// </summary>
    /// <param name="after">The later stats instance.</param>
    /// <param name="before">The earlier stats instance.</param>
    /// <returns>The difference in stats (delta).</returns>
    public static TransformStatsDelta operator -(
        TransformStats after,
        TransformStats before) => after.Diff(before);
}