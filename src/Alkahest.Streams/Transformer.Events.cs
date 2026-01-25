using System.Threading.Channels;

namespace Alkahest.Streams;

using Events;
using Performance;

public abstract partial class Transformer : ITransformer
{
    /// <summary>
    /// Occurs when a transform process begins.
    /// </summary>
    public event EventHandler<TransformBeginEventArgs>? TransformProcessBegin;

    /// <summary>
    /// Occurs when a handler process begins.
    /// </summary>
    public event EventHandler<HandlerBeginEventArgs>? HandlerProcessBegin;

    /// <summary>
    /// Occurs when a handler process ends.
    /// </summary>
    public event EventHandler<HandlerEndEventArgs>? HandlerProcessEnd;

    /// <summary>
    /// Occurs when a transform process ends.
    /// </summary>
    public event EventHandler<TransformEndEventArgs>? TransformProcessEnd;

    /// <summary>
    /// Channel for high-throughput, non-blocking metrics capture. Consumers can read transform events asynchronously.
    /// </summary>
    protected Channel<TransformEventArgs> MetricsChannel { get; } = Channel.CreateUnbounded<TransformEventArgs>(
            new UnboundedChannelOptions() { SingleWriter = false, SingleReader = true }
    );

    /// <summary>
    /// Reader for high-throughput, non-blocking metrics consumption.
    /// </summary>
    public ChannelReader<TransformEventArgs> MetricsChannelReader => MetricsChannel.Reader;

    /// <summary>
    /// Notifies that a transform process is beginning.
    /// </summary>
    /// <param name="inputLength">The length of the input bytes.</param>
    protected async Task NotifyTransformStartAsync(long inputLength = -1)
    {
        var args = new TransformBeginEventArgs
        {
            InputLength = inputLength
        };
        MetricsChannel.Writer.TryWrite(args);

        await Task.Run(() =>
        {
            try
            {
                TransformProcessBegin?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Alkahest Event Handler Error] {nameof(TransformProcessBegin)}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        });
    }

    /// <summary>
    /// Notifies that a handler process is beginning.
    /// </summary>
    /// <param name="handler">The transform handler that is starting.</param>
    /// <param name="handlerIndex">The index of the handler in the transform pipeline.</param>
    protected async Task NotifyHandlerStartAsync(ITransformHandler handler, int handlerIndex)
    {
        var args = new HandlerBeginEventArgs(
            handler.Name,
            handlerIndex
        );

        MetricsChannel.Writer.TryWrite(args);

        await Task.Run(() =>
        {
            try
            {
                HandlerProcessBegin?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Alkahest Event Handler Error] {nameof(HandlerProcessBegin)}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        });
    }

    /// <summary>
    /// Collects performance statistics 
    /// </summary>
    /// <param name="handlerName"></param>
    /// <param name="handlerIndex"></param>
    /// <param name="duration"></param>
    /// <param name="bytesProcessed"></param>
    /// <param name="exception"></param>
    /// <param name="statsBefore"></param>
    /// <param name="statsAfter"></param>
    protected async Task NotifyHandlerEndAsync(
        string handlerName,
        int handlerIndex,
        TimeSpan duration,
        long bytesProcessed,
        Exception? exception = null,
        TransformStats? statsBefore = null,
        TransformStats? statsAfter = null)
    {
        var delta = statsAfter != null && statsBefore != null
            ? statsAfter - statsBefore
            : null;

        if (delta != null)
            HandlerDeltas?.Add(delta);

        var args = new HandlerEndEventArgs(
            handlerName,
            handlerIndex,
            duration,
            bytesProcessed,
            exception,
            statsBefore,
            statsAfter,
            delta);

        MetricsChannel.Writer.TryWrite(args);

        await Task.Run(() =>
        {
            try
            {
                HandlerProcessEnd?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Alkahest Event Handler Error] {nameof(HandlerProcessEnd)}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="totalDuration"></param>
    /// <param name="totalBytesIn"></param>
    /// <param name="totalBytesOut"></param>
    /// <param name="exception"></param>
    protected async Task NotifyTransformEndAsync(
        TimeSpan totalDuration,
        long totalBytesIn,
        long totalBytesOut,
        Exception? exception = null)
    {
        var totalStats = CapturePerformanceStats(totalDuration, totalBytesIn, totalBytesOut);
        var aggregates = HandlerDeltas?.ComputeAggregates();

        var args = new TransformEndEventArgs(
            totalDuration,
            totalBytesIn,
            totalBytesOut,
            exception,
            totalStats,
            aggregates,
            HandlerDeltas);

        MetricsChannel.Writer.TryWrite(args);
        
        await Task.Run(() => {
            try
            {
                TransformProcessEnd?.Invoke(this, args);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[Alkahest Event Handler Error] {nameof(TransformProcessEnd)}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        });


        // Clear for next run
        HandlerDeltas?.Clear();
    }
}
