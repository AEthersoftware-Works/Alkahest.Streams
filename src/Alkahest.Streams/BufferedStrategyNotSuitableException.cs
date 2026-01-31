namespace Alkahest.Streams;

/// <summary>
/// Thrown when attempting to use the buffered processing strategy on a stream that is too large,
/// which could lead to excessive memory usage or out-of-memory exceptions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the exception with the offending stream length.
/// <para>
/// This exception is thrown as a safety mechanism to guide users toward the pipelined strategy
/// for large or unknown-size streams. The threshold is deliberately set below the .NET array
/// maximum length to leave headroom for temporary allocations and GC pressure.
/// </para>
/// </remarks>
/// <param name="streamLength">The length of the stream that exceeded the safe threshold.</param>
[Serializable]
public sealed class BufferedStrategyNotSuitableException(long streamLength) : InvalidOperationException($"Buffered strategy is not suitable for streams of size {streamLength:N0} bytes (>1.5 GB). " +
                   "Use Pipelined strategy for large or unknown-size data to avoid memory issues.")
{
    /// <summary>
    /// The actual length of the input stream that triggered the exception.
    /// </summary>
    public long StreamLength { get; } = streamLength;

    /// <summary>
    /// Throws the exception if the stream length exceeds the safe threshold.
    /// </summary>
    /// <param name="value">The length of the stream.</param>
    /// <param name="maxLength">The maximum safe length in bytes (default is 1.5 GB).</param>
    /// <exception cref="BufferedStrategyNotSuitableException">Thrown when the stream is too large.</exception>
    public static void ThrowIfStreamLengthGreater(long value, long maxLength = 1_610_612_736)
    {
        if (value > maxLength)
            throw new BufferedStrategyNotSuitableException(value);
    }
}
