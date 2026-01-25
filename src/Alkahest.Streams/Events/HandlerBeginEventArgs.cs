namespace Alkahest.Streams.Events;

/// <summary>
/// Provides data for the handler begin event.
/// </summary>
public class HandlerBeginEventArgs(string handlerName, int handlerIndex) : HandlerEventArgs(handlerName, handlerIndex)
{
}

