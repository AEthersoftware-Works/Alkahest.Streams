namespace Alkahest.Streams;

/// <summary>
/// Defines the strategies for handling stream transformations.
/// </summary>
public enum HandlerStrategy
{
    /// <summary>
    /// Automatically selects Buffered for small streams or Pipelined for large ones.
    /// </summary>
    Auto,
    /// <summary>
    /// Uses a buffered approach, processing handlers sequentially with intermediate buffering.
    /// </summary>
    Buffered,
    /// <summary>
    /// Uses a pipelined approach for efficient processing of large streams.
    /// </summary>
    Pipelined
}
