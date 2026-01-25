namespace Alkahest.Streams.Events;

/// <summary>
/// Provides data for the transform begin event.
/// </summary>
public class TransformBeginEventArgs : TransformEventArgs
{
    /// <summary>
    /// Gets the timestamp when the transform began.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the length of the input stream, if known; otherwise -1.
    /// </summary>
    public long InputLength { get; init; } // If known (stream.CanSeek), else -1
}

