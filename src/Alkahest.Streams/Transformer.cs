using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;

namespace Alkahest.Streams;

using Events;
using Performance;

/// <summary>
/// Abstract base class for transformers that implement stream processing with handlers.
/// </summary>
public abstract partial class Transformer : ITransformer
{
    // TODO: Add means to "fire" telemetry anonymous function

    /// <summary>
    /// Gets or sets the memory handling strategy used for processing handlers.
    /// </summary>
    protected HandlerStrategy Strategy { get; set; } = HandlerStrategy.Auto;

    /// <summary>
    /// The chain of handlers to process the stream.
    /// </summary>
    protected List<ITransformHandler> Handlers { get; init; } = [];

    /// <summary>
    /// Creates a transformer instance with the specified handlers.
    /// </summary>
    /// <typeparam name="TTransformer">The type of the transformer.</typeparam>
    /// <param name="handlers">The list of transform handlers.</param>
    /// <returns>A new transformer instance.</returns>
    public static TTransformer WithHandlers<TTransformer>(List<ITransformHandler> handlers) where TTransformer : Transformer, ITransformer, new()
    {
        var instance = new TTransformer() { Handlers = handlers };
        return instance;
    }

    /// <summary>
    /// Creates a transformer instance with the specified handlers and strategy.
    /// </summary>
    /// <typeparam name="TTransformer">The type of the transformer.</typeparam>
    /// <param name="handlers">The list of transform handlers.</param>
    /// <param name="strategy">The handler strategy to use.</param>
    /// <returns>A new transformer instance.</returns>
    public static TTransformer WithHandlersAndStrategy<TTransformer>(List<ITransformHandler> handlers, HandlerStrategy strategy) where TTransformer : Transformer, ITransformer, new()
    {
        var instance = new TTransformer() { Handlers = handlers, Strategy = strategy };
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
            if (input.CanSeek && input.Length < 5 * 1024 * 1024) // 5 MB threshold
                Strategy = HandlerStrategy.Buffered;
            else
                Strategy = HandlerStrategy.Pipelined;
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

        // Pre-allocate the list
        HandlerDeltas = new HandlerStatsDeltaCollection(Handlers.Count);

        Stream current = input;
        long currentInputLength = totalBytesIn;
        int handlerIdx = 0;

        foreach (var handler in Handlers)
        {
            var handlerStart = Stopwatch.StartNew();

            await NotifyHandlerStartAsync(handler, handlerIdx);

            var handlerStatsBefore = CapturePerformanceStats(TimeSpan.Zero);

            long handlerBytesIn = currentInputLength;

            try
            {
                // Starting with 256KB capacity to reduce resizes
                using var tempOutput = new PooledMemoryStream(bufferSize * 4);
                await handler.ProcessAsync(current, tempOutput);

                long handlerBytesOut = tempOutput.Length;
                totalBytesOut += handlerBytesOut;

                var duration = handlerStart.Elapsed;
                var handlerStatsAfter = CapturePerformanceStats(duration, totalBytesIn, totalBytesOut);
                var handlerStatsDelta = handlerStatsAfter - handlerStatsBefore;

                HandlerDeltas.Add(handlerStatsDelta);

                HandlerProcessEnd?.Invoke(this, new HandlerEndEventArgs(
                    handlerName: handler.Name,
                    handlerIndex: handlerIdx,
                    duration: duration,
                    bytesProcessed: handlerBytesOut,
                    statsBefore: handlerStatsBefore,
                    statsAfter: handlerStatsAfter,
                    statsDelta: handlerStatsDelta));

                tempOutput.Position = 0;

                // Dispose previous stream.
                if (current != input)
                    await current.DisposeAsync();

                current = tempOutput;
                currentInputLength = handlerBytesOut;

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
                    statsAfter: handlerStatsAfter
                );

                throw;
            }

            handlerIdx++;
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
                        await handler.ProcessAsync(currentInput, pipe.Writer.AsStream());

                        var duration = handlerStart.Elapsed;
                        var handlerStatsAfter = CapturePerformanceStats(duration, totalBytesIn, totalBytesOut);

                        await NotifyHandlerEndAsync(
                            handlerName: handler.Name,
                            handlerIndex: handlerIdx,
                            duration: duration,
                            bytesProcessed: -1,
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
                            exception: ex,
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
