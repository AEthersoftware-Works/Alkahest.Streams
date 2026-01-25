using System;
using System.Collections;

namespace Alkahest.Streams.Performance;

/// <summary>
/// A collection of performance statistics deltas for a handler, providing efficient aggregation capabilities.
/// </summary>
public sealed class HandlerStatsDeltaCollection(int capacity) : IReadOnlyList<TransformStatsDelta>
{
    private readonly List<TransformStatsDelta> _deltas = new(capacity);

    /// <summary>
    /// Gets the number of deltas in the collection.
    /// </summary>
    public int Count => _deltas.Count;

    /// <summary>
    /// Gets the delta at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the delta to get.</param>
    /// <returns>The delta at the specified index.</returns>
    public TransformStatsDelta this[int index] => _deltas[index];

    /// <summary>
    /// Adds a performance statistics delta to the collection.
    /// </summary>
    /// <param name="delta">The delta to add.</param>
    public void Add(TransformStatsDelta delta) => _deltas.Add(delta);

    /// <summary>
    /// Removes all deltas from the collection.
    /// </summary>
    public void Clear() => _deltas.Clear();

    /// <summary>
    /// Computes aggregated performance statistics from all deltas in the collection.
    /// </summary>
    /// <returns>A HandlerStatsAggregate containing the computed aggregates.</returns>
    public HandlerStatsAggregate ComputeAggregates()
    {
        if (_deltas.Count == 0)
            return new HandlerStatsAggregate();

        // Initialize sums, mins, maxes for all properties
        long sumPeakWS = 0, minPeakWS = long.MaxValue, maxPeakWS = long.MinValue;
        long sumWorkingSet = 0, minWorkingSet = long.MaxValue, maxWorkingSet = long.MinValue;
        long sumPrivateMem = 0, minPrivateMem = long.MaxValue, maxPrivateMem = long.MinValue;
        long sumGCAlloc = 0, minGCAlloc = long.MaxValue, maxGCAlloc = long.MinValue;
        long sumGCTotalMem = 0, minGCTotalMem = long.MaxValue, maxGCTotalMem = long.MinValue;
        long sumGCCountGen0 = 0, minGCCountGen0 = int.MaxValue, maxGCCountGen0 = int.MinValue;
        long sumGCCountGen1 = 0, minGCCountGen1 = int.MaxValue, maxGCCountGen1 = int.MinValue;
        long sumGCCountGen2 = 0, minGCCountGen2 = int.MaxValue, maxGCCountGen2 = int.MinValue;
        double sumProcessorMs = 0, minProcessorMs = double.MaxValue, maxProcessorMs = double.MinValue;
        long sumThreadCount = 0, minThreadCount = int.MaxValue, maxThreadCount = int.MinValue;
        long sumBytesIn = 0, minBytesIn = long.MaxValue, maxBytesIn = long.MinValue;
        long sumBytesOut = 0, minBytesOut = long.MaxValue, maxBytesOut = long.MinValue;
        double sumDurationMs = 0, minDurationMs = double.MaxValue, maxDurationMs = double.MinValue;

        foreach (var delta in _deltas)
        {
            // Memory metrics
            sumPeakWS += delta.DeltaPeakWorkingSetBytes;
            minPeakWS = Math.Min(minPeakWS, delta.DeltaPeakWorkingSetBytes);
            maxPeakWS = Math.Max(maxPeakWS, delta.DeltaPeakWorkingSetBytes);

            sumWorkingSet += delta.DeltaWorkingSetBytes;
            minWorkingSet = Math.Min(minWorkingSet, delta.DeltaWorkingSetBytes);
            maxWorkingSet = Math.Max(maxWorkingSet, delta.DeltaWorkingSetBytes);

            sumPrivateMem += delta.DeltaPrivateMemoryBytes;
            minPrivateMem = Math.Min(minPrivateMem, delta.DeltaPrivateMemoryBytes);
            maxPrivateMem = Math.Max(maxPrivateMem, delta.DeltaPrivateMemoryBytes);

            sumGCAlloc += delta.DeltaGCAllocatedBytes;
            minGCAlloc = Math.Min(minGCAlloc, delta.DeltaGCAllocatedBytes);
            maxGCAlloc = Math.Max(maxGCAlloc, delta.DeltaGCAllocatedBytes);

            sumGCTotalMem += delta.DeltaGCTotalMemoryBytes;
            minGCTotalMem = Math.Min(minGCTotalMem, delta.DeltaGCTotalMemoryBytes);
            maxGCTotalMem = Math.Max(maxGCTotalMem, delta.DeltaGCTotalMemoryBytes);

            // GC counts
            sumGCCountGen0 += delta.DeltaGCCountGen0;
            minGCCountGen0 = Math.Min(minGCCountGen0, delta.DeltaGCCountGen0);
            maxGCCountGen0 = Math.Max(maxGCCountGen0, delta.DeltaGCCountGen0);

            sumGCCountGen1 += delta.DeltaGCCountGen1;
            minGCCountGen1 = Math.Min(minGCCountGen1, delta.DeltaGCCountGen1);
            maxGCCountGen1 = Math.Max(maxGCCountGen1, delta.DeltaGCCountGen1);

            sumGCCountGen2 += delta.DeltaGCCountGen2;
            minGCCountGen2 = Math.Min(minGCCountGen2, delta.DeltaGCCountGen2);
            maxGCCountGen2 = Math.Max(maxGCCountGen2, delta.DeltaGCCountGen2);

            // CPU and threading
            double procMs = delta.DeltaProcessorTime.TotalMilliseconds;
            sumProcessorMs += procMs;
            minProcessorMs = Math.Min(minProcessorMs, procMs);
            maxProcessorMs = Math.Max(maxProcessorMs, procMs);

            sumThreadCount += delta.DeltaThreadCount;
            minThreadCount = Math.Min(minThreadCount, delta.DeltaThreadCount);
            maxThreadCount = Math.Max(maxThreadCount, delta.DeltaThreadCount);

            // Data
            sumBytesIn += delta.DeltaBytesIn;
            minBytesIn = Math.Min(minBytesIn, delta.DeltaBytesIn);
            maxBytesIn = Math.Max(maxBytesIn, delta.DeltaBytesIn);

            sumBytesOut += delta.DeltaBytesOut;
            minBytesOut = Math.Min(minBytesOut, delta.DeltaBytesOut);
            maxBytesOut = Math.Max(maxBytesOut, delta.DeltaBytesOut);

            // Duration
            double durMs = delta.Duration.TotalMilliseconds;
            sumDurationMs += durMs;
            minDurationMs = Math.Min(minDurationMs, durMs);
            maxDurationMs = Math.Max(maxDurationMs, durMs);
        }

        int count = _deltas.Count;

        return new HandlerStatsAggregate
        {
            TotalInvocations = count,

            AvgDeltaPeakWorkingSetBytes = (double)sumPeakWS / count,
            MinDeltaPeakWorkingSetBytes = minPeakWS,
            MaxDeltaPeakWorkingSetBytes = maxPeakWS,

            AvgDeltaPrivateMemoryBytes = (double)sumPrivateMem / count,
            MinDeltaPrivateMemoryBytes = minPrivateMem,
            MaxDeltaPrivateMemoryBytes = maxPrivateMem,

            AvgDeltaProcessorTimeMs = sumProcessorMs / count,
            MinDeltaProcessorTimeMs = minProcessorMs,
            MaxDeltaProcessorTimeMs = maxProcessorMs,

            AvgDeltaTimeMs = sumDurationMs / count,
            MinDeltaTimeMs = minDurationMs,
            MaxDeltaTimeMs = maxDurationMs,

            AvgDeltaBytesIn = (double)sumBytesIn / count,
            MinDeltaBytesIn = minBytesIn,
            MaxDeltaBytesIn = maxBytesIn,

            AvgDeltaBytesOut = (double)sumBytesOut / count,
            MinDeltaBytesOut = minBytesOut,
            MaxDeltaBytesOut = maxBytesOut,

            AvgDeltaGCCountGen0 = (double)sumGCCountGen0 / count,
            AvgDeltaGCCountGen1 = (double)sumGCCountGen1 / count,
            AvgDeltaGCCountGen2 = (double)sumGCCountGen2 / count,
        };
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<TransformStatsDelta> GetEnumerator() => _deltas.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _deltas.GetEnumerator();
}