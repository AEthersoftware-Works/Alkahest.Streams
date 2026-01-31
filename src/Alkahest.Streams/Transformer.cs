using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;

namespace Alkahest.Streams;

using Performance;

/// <summary>
/// Abstract base class for transformers that implement stream processing with handlers.
/// </summary>
public abstract partial class Transformer : ITransformer
{
    /// <summary>
    /// The default threshold (25 MB) for using to the Buffered strategy in Auto mode.
    /// </summary>
    protected const long BufferedStreamThresholdDefault = 26_214_400; //25 MB -> 25 * 1024 * 1024
    
    /// <summary>
    /// The maximum safe length (1.5 GB) for the Buffered strategy to prevent memory issues.
    /// </summary>
    protected const long BufferedStreamMaxLength = 1_610_612_736; // 1.5 GB -> 1.5 * 1024 * 1024 * 1024

    // TODO: Add means to "fire" telemetry anonymous function

    /// <summary>
    /// Gets or sets the threshold in bytes for using to the Buffered strategy when using Auto mode.
    /// Values exceeding 1.5 GB will throw <see cref="BufferedStrategyNotSuitableException"/>.
    /// </summary>
    /// <exception cref="BufferedStrategyNotSuitableException">Thrown if the value exceeds the maximum safe length.</exception>
    protected long BufferedStreamThreshold
    {
        get;
        set
        {
            BufferedStrategyNotSuitableException.ThrowIfStreamLengthGreater(value, BufferedStreamMaxLength);
            field = value;
        }
    } = BufferedStreamThresholdDefault;

    /// <summary>
    /// Gets or sets the memory handling strategy used for processing handlers.
    /// </summary>
    protected HandlerStrategy Strategy { get; set; } = HandlerStrategy.Auto;

    /// <summary>
    /// The chain of handlers to process the stream.
    /// </summary>
    protected List<ITransformHandler> Handlers { get; init; } = [];

    // NOTE: If this grows beyond 3 parameters, a configuration class should be considered.
    /// <inheritdoc/>
    public static TTransformer Create<TTransformer>(
        List<ITransformHandler> handlers,
        HandlerStrategy strategy = HandlerStrategy.Auto,
        long bufferedPoolThreshold = BufferedStreamThresholdDefault
    ) where TTransformer : Transformer, ITransformer, new()
    {
        BufferedStrategyNotSuitableException.ThrowIfStreamLengthGreater(bufferedPoolThreshold);
        var instance = new TTransformer()
        {
            Handlers = handlers,
            Strategy = strategy,
            BufferedStreamThreshold = bufferedPoolThreshold
        };

        return instance;
    }

    /// <summary>
    /// Processes the input stream through the configured transformation handlers and writes the result to the output stream.
    /// </summary>
    /// <param name="input">The input stream to process.</param>
    /// <param name="output">The output stream to write the transformed data to.</param>
    /// <param name="bufferSize">The buffer size to use for processing. Default is 65536 bytes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessAsync(Stream input, Stream output, int bufferSize = 65536)
    {
        if (Strategy == HandlerStrategy.Auto)
        {
            Strategy = input.CanSeek && input.Length <= BufferedStreamThreshold
                ? HandlerStrategy.Buffered
                : HandlerStrategy.Pipelined;
        }

        switch (Strategy)
        {
            case HandlerStrategy.Buffered:
                await ProcessBufferedAsync(input, output, bufferSize);
                break;
            case HandlerStrategy.Pipelined:
                await ProcessPipelineAsync(input, output, bufferSize);
                break;
        }
    }

    /// <summary>
    /// Processes the input stream using a buffered strategy, applying each handler sequentially and buffering intermediate results.
    /// </summary>
    /// <param name="input">The input stream to process.</param>
    /// <param name="output">The output stream to write the final result to.</param>
    /// <param name="bufferSize">The buffer size to use for processing. Default is 65536 bytes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ProcessBufferedAsync(Stream input, Stream output, int bufferSize = 65536)
    {
        if (input.CanSeek)
            BufferedStrategyNotSuitableException.ThrowIfStreamLengthGreater(input.Length, BufferedStreamMaxLength);

        var startTime = Stopwatch.StartNew();
        long totalBytesIn = input.CanSeek ? input.Length : 0;
        long totalBytesOut = 0;
        Exception? lastError = null;

        await NotifyTransformStartAsync(totalBytesIn);

        if (Handlers.Count == 0)
        {
            await input.CopyWithPooledBufferAsync(output, bufferSize);
            return;
        }

        Stream current = input;
        long currentInputLength = totalBytesIn;

        try
        {
            // Pre-allocate the list
            HandlerDeltas = new HandlerStatsDeltaCollection(Handlers.Count);

            int handlerIdx = 0;

            foreach (var handler in Handlers)
            {
                var handlerStart = Stopwatch.StartNew();

                await NotifyHandlerStartAsync(handler, handlerIdx);

                var handlerStatsBefore = CapturePerformanceStats(TimeSpan.Zero);

                try
                {
                    // Starting with 256KB capacity to reduce resizes
                    var tempOutput = new PooledMemoryStream(bufferSize * 4);
                    await handler.ProcessAsync(current, tempOutput);

                    long handlerBytesOut = tempOutput.Length;
                    totalBytesOut += handlerBytesOut;

                    var duration = handlerStart.Elapsed;
                    var handlerStatsAfter = CapturePerformanceStats(duration, totalBytesIn, totalBytesOut);

                    await NotifyHandlerEndAsync(
                        handlerName: handler.Name,
                        handlerIndex: handlerIdx,
                        duration: duration,
                        bytesProcessed: handlerBytesOut,
                        statsBefore: handlerStatsBefore,
                        statsAfter: handlerStatsAfter
                    );

                    tempOutput.Position = 0;

                    // Dispose previous stream.
                    if (current != input)
                        await current.DisposeAsync();

                    current = tempOutput;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    var duration = handlerStart.Elapsed;
                    var handlerStatsAfter = CapturePerformanceStats(duration, totalBytesIn, totalBytesOut);

                    await NotifyHandlerEndAsync(
                        handlerName: handler.Name,
                        handlerIndex: handlerIdx,
                        duration: duration,
                        bytesProcessed: -1,
                        statsBefore: handlerStatsBefore,
                        statsAfter: handlerStatsAfter,
                        exception: lastError
                    );

                    throw;
                }
                handlerIdx++;
            }
        }
        catch (Exception)
        {
            var totalDuration = startTime.Elapsed;

            await NotifyTransformEndAsync(
                totalDuration: totalDuration,
                totalBytesIn: totalBytesIn,
                totalBytesOut: totalBytesOut,
                exception: lastError
            );

            throw;
        }

        try
        {
            // Final copy to output using pooled buffer
            await current.CopyWithPooledBufferAsync(output);
        }
        finally
        {
            var totalDuration = startTime.Elapsed;

            await NotifyTransformEndAsync(
                totalDuration: totalDuration,
                totalBytesIn: totalBytesIn,
                totalBytesOut: totalBytesOut,
                exception: lastError
            );

            // Dispose the last intermediate stream
            if (current != input)
                await current.DisposeAsync();
        }
    }

    /// <summary>
    /// Processes the input stream using a pipelined strategy, chaining handlers in a pipeline for efficient processing of large streams.
    /// </summary>
    /// <param name="input">The input stream to process.</param>
    /// <param name="output">The output stream to write the final result to.</param>
    /// <param name="bufferSize">The buffer size to use for processing. Default is 65536 bytes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task ProcessPipelineAsync(Stream input, Stream output, int bufferSize = 65536)
    {
        // TODO: Get configuration options to capture this
        var startTime = Stopwatch.StartNew();
        long totalBytesIn = 0;
        long totalBytesOut = 0;
        Exception? lastError = null;

        await NotifyTransformStartAsync(input.CanSeek ? input.Length : -1);

        if (Handlers.Count == 0)
        {
            await input.CopyWithPooledBufferAsync(output);
            return;
        }

        // Create a pipeline to chain the transformations
        var pipe = new Pipe(new PipeOptions(
            pool: MemoryPool<byte>.Shared,
            minimumSegmentSize: bufferSize,
            pauseWriterThreshold: bufferSize * 4,
            resumeWriterThreshold: bufferSize * 2));

        _ = Task.Run(async () =>
        {
            try
            {
                HandlerDeltas = new HandlerStatsDeltaCollection(Handlers.Count);

                Stream currentInput = input;
                int handlerIdx = 0;

                // DO NOT DEQUEUE for future use of the Transformer instance! 
                foreach (var handler in Handlers)
                {
                    var handlerStart = Stopwatch.StartNew();

                    await NotifyHandlerStartAsync(handler, handlerIdx);

                    var handlerStatsBefore = CapturePerformanceStats(TimeSpan.Zero);

                    try
                    {
                        var tempOutput = new TrackingStreamWriter(pipe.Writer.AsStream(true));
                        await handler.ProcessAsync(currentInput, tempOutput);

                        long handlerBytesOut = tempOutput.BytesWritten;
                        totalBytesOut += handlerBytesOut;

                        var duration = handlerStart.Elapsed;
                        var handlerStatsAfter = CapturePerformanceStats(duration, totalBytesIn, totalBytesOut);

                        await NotifyHandlerEndAsync(
                            handlerName: handler.Name,
                            handlerIndex: handlerIdx,
                            duration: duration,
                            bytesProcessed: handlerBytesOut,
                            statsBefore: handlerStatsBefore,
                            statsAfter: handlerStatsAfter
                        );

                        if (currentInput != input)
                            await currentInput.DisposeAsync();

                        currentInput = pipe.Writer.AsStream();
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        
                        var duration = handlerStart.Elapsed;
                        var handlerStatsAfter = CapturePerformanceStats(duration, totalBytesIn, totalBytesOut);

                        await NotifyHandlerEndAsync(
                            handlerName: handler.Name,
                            handlerIndex: handlerIdx,
                            duration: duration,
                            bytesProcessed: -1,
                            exception: lastError,
                            statsBefore: handlerStatsBefore,
                            statsAfter: handlerStatsAfter
                        );

                        throw;
                    }

                    handlerIdx++;
                }

                await pipe.Writer.CompleteAsync();
            }
            catch (Exception ex)
            {
                await pipe.Writer.CompleteAsync(ex);

                var totalDuration = startTime.Elapsed;

                await NotifyTransformEndAsync(
                    totalDuration: totalDuration,
                    totalBytesIn: totalBytesIn,
                    totalBytesOut: totalBytesOut,
                    exception: lastError
                );
            }
        });

        // Gather final stats...

        try
        {
            await pipe.Reader.CopyToAsync(output);
        }
        finally
        {
            await pipe.Reader.CompleteAsync();

            var totalDuration = startTime.Elapsed;

            await NotifyTransformEndAsync(
                totalDuration: totalDuration,
                totalBytesIn: totalBytesIn,
                totalBytesOut: totalBytesOut,
                exception: lastError
            );
        }
    }
}
