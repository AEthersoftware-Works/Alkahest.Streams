using Alkahest.Streams.Performance;

using Xunit.Internal;

namespace Alkahest.Streams.Tests.Mock;

public class TestTransformer : Transformer
{
    public HandlerStrategy GetHandlerStrategy() => Strategy;

    public List<ITransformHandler> GetHandlers() => Handlers;

    public HandlerStatsDeltaCollection? GetHandlerDeltas() => HandlerDeltas;

    public void InitHandlerDeltas(int capacity = 0) => HandlerDeltas = new HandlerStatsDeltaCollection(capacity);

    public void AppendHandlerDelta(TransformStatsDelta delta)
    {
        if (HandlerDeltas == null)
            HandlerDeltas = new HandlerStatsDeltaCollection(1);

        HandlerDeltas.Add(delta);
    }

    public new async Task NotifyTransformStartAsync(long inputLength = -1)
        => await base.NotifyTransformStartAsync(inputLength);

    public new async Task NotifyHandlerStartAsync(ITransformHandler handler, int handlerIndex)
        => await base.NotifyHandlerStartAsync(handler, handlerIndex);

    public new async Task NotifyHandlerEndAsync(
        string handlerName,
        int handlerIndex,
        TimeSpan duration,
        long bytesProcessed,
        Exception? exception = null,
        TransformStats? statsBefore = null,
        TransformStats? statsAfter = null)
        => await base.NotifyHandlerEndAsync(handlerName, handlerIndex, duration, bytesProcessed, exception, statsBefore, statsAfter);

    public new async Task NotifyTransformEndAsync(
        TimeSpan totalDuration,
        long totalBytesIn,
        long totalBytesOut,
        Exception? exception = null)
        => await base.NotifyTransformEndAsync(totalDuration, totalBytesIn, totalBytesOut, exception);

    public new static TransformStats CapturePerformanceStats(TimeSpan duration, long bytesIn = 0, long bytesOut = 0)
        => Transformer.CapturePerformanceStats(duration, bytesIn, bytesOut);
}
