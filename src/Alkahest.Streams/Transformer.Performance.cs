using System.Diagnostics;

namespace Alkahest.Streams;

using Performance;

public abstract partial class Transformer : ITransformer
{
    /// <summary>
    /// The performance deltas captured during stream handler processing. Used metrics capture/analysis.
    /// </summary>
    protected HandlerStatsDeltaCollection? HandlerDeltas { get; set; } = null;

    /// <summary>
    /// Captures the current performance statistics of the process.
    /// </summary>
    /// <param name="duration">The duration of the operation.</param>
    /// <param name="bytesIn">The number of bytes read.</param>
    /// <param name="bytesOut">The number of bytes written.</param>
    /// <returns>The captured performance statistics.</returns>
    protected static TransformStats CapturePerformanceStats(TimeSpan duration, long bytesIn = 0, long bytesOut = 0)
    {
        var process = Process.GetCurrentProcess();

        return new TransformStats
        {
            PeakWorkingSetBytes = process.PeakWorkingSet64,
            WorkingSetBytes = process.WorkingSet64,
            PrivateMemoryBytes = process.PrivateMemorySize64,
            GCAllocatedBytes = GC.GetTotalAllocatedBytes(),
            GCTotalMemoryBytes = GC.GetTotalMemory(true),

            GCCountGen0 = GC.CollectionCount(0),
            GCCountGen1 = GC.CollectionCount(1),
            GCCountGen2 = GC.CollectionCount(2),

            TotalProcessorTime = process.TotalProcessorTime,
            ThreadCount = process.Threads.Count,

            BytesIn = bytesIn,
            BytesOut = bytesOut,
            Duration = duration
        };
    }

}
