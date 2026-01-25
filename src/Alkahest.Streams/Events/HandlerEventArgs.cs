using System;

namespace Alkahest.Streams.Events;

/// <summary>
/// The base class for handler events arguments.
/// </summary>
public class HandlerEventArgs(string handlerName, int handlerIndex) : TransformEventArgs
{
    /// <summary>
    /// Gets the timestamp when the handler began processing.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    public string HandlerName { get; init; } = handlerName;

    /// <summary>
    /// Gets the index of the handler in the processing queue.
    /// </summary>
    public int HandlerIndex { get; init; } = handlerIndex;
}
