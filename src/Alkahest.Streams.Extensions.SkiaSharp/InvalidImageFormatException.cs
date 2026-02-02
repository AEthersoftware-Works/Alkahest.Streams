namespace Alkahest.Streams.Extensions.SkiaSharp;

/// <summary>
/// Exception thrown when the detected image format does not match the expected format.
/// </summary>
/// <remarks>
/// Initializes a new instance with the specified message.
/// </remarks>
/// <param name="message">The error message.</param>
public class InvalidImageFormatException(string message) : Exception(message)
{
}