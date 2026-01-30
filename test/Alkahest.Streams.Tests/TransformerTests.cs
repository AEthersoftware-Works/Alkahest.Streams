using System.Security.Cryptography;
using System.Text;

using Alkahest.Streams.Performance;

namespace Alkahest.Streams.Tests;

using System.Runtime;

using Alkahest.Streams.Events;

using Mock;

using Xunit.Internal;

public class TransformerTests
{
    [Fact]
    public async Task PassThruHandler_Appends_Stream()
    {
        // Arrange
        // h/t Scott Brady<scott@scottbrady91.com> https://www.scottbrady.io/
        // got more soul than a sock with a hole

        string messagePrefix = "got more soul than";
        string messageSuffix = "a sock with a hole";

        byte[] messageBytes = Encoding.UTF8.GetBytes(messagePrefix);

        var options = new PassThruOptions(completionMessage: $" {messageSuffix}");
        var transformer = new TransformBuilder()
            .ProcessWith<PassThruHandler, PassThruOptions>(options)
            .Build();

        using var input = new MemoryStream(messageBytes);
        using var output = new MemoryStream();

        // Act
        await transformer.ProcessAsync(input, output);
        output.Position = 0;

        using var sr = new StreamReader(output);
        var result = await sr.ReadToEndAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal($"{messagePrefix} {messageSuffix}", result);
    }

    [Fact]
    public async Task Transformer_Will_Passthrough_Stream_Without_Handlers()
    {
        // Arrange
        var emptyTransformer = new TransformBuilder()
            .Build();

        var originalBytes = Enumerable.Range(0, 0x1234)
            .Select(b => (byte)(b % 20)).ToArray();

        var input = new MemoryStream(originalBytes);
        var output = new MemoryStream();

        // Act
        await emptyTransformer.ProcessAsync(input, output);

        // Assert
        var outBytes = output.ToArray();
        Assert.True(originalBytes.SequenceEqual(outBytes));
    }

    [Fact]
    public async Task PassThruHandler_Appends_Stream_With_Pipelined_Strategy()
    {
        // Arrange
        // h/t Scott Brady<scott@scottbrady91.com> https://www.scottbrady.io/
        // got more soul than a sock with a hole

        string messagePrefix = "got more soul than";
        string messageSuffix = "a sock with a hole";

        byte[] messageBytes = Encoding.UTF8.GetBytes(messagePrefix);

        var options = new PassThruOptions(completionMessage: $" {messageSuffix}");
        var transformer = new TransformBuilder()
            .ProcessWith<PassThruHandler, PassThruOptions>(options)
            .UseStrategy(HandlerStrategy.Pipelined)
            .Build();

        using var input = new MemoryStream(messageBytes);
        using var output = new MemoryStream();

        // Act
        await transformer.ProcessAsync(input, output);
        output.Position = 0;

        using var sr = new StreamReader(output);
        var result = await sr.ReadToEndAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal($"{messagePrefix} {messageSuffix}", result);
    }

    [Fact]
    public async Task Transformer_Will_Passthrough_Stream_Without_Handlers_With_Pipelined_Strategy()
    {
        // Arrange
        var emptyTransformer = new TransformBuilder()
            .UseStrategy(HandlerStrategy.Pipelined)
            .Build();

        var originalBytes = Enumerable.Range(0, 0x1234)
            .Select(b => (byte)(b % 20)).ToArray();

        var input = new MemoryStream(originalBytes);
        var output = new MemoryStream();

        // Act
        await emptyTransformer.ProcessAsync(input, output);

        // Assert
        var outBytes = output.ToArray();
        Assert.True(originalBytes.SequenceEqual(outBytes));
    }

    [Fact]
    public async Task Events_Triggered()
    {
        // Assemble
        TestTransformer transformer = new TransformBuilder()
            .Build<TestTransformer>();

        // Act
        var transformBeginEvent = await Assert.RaisesAnyAsync<TransformBeginEventArgs>(
            eh => transformer.TransformProcessBegin += eh,
            eh => transformer.TransformProcessBegin -= eh,
            async () => await transformer.NotifyTransformStartAsync(0));

        transformer.InitHandlerDeltas();

        var handlerBeginEvent = await Assert.RaisesAnyAsync<HandlerBeginEventArgs>(
            eh => transformer.HandlerProcessBegin += eh,
            eh => transformer.HandlerProcessBegin -= eh,
            async () =>
            {
                var handler = new StallingHandler();
                await transformer.NotifyHandlerStartAsync(handler, 0);
            }
        );

        TransformStats? handlerStatsBefore = null,
                        handlerStatsAfter = null;

        var handlerEndEvent = await Assert.RaisesAnyAsync<HandlerEndEventArgs>(
            eh => transformer.HandlerProcessEnd += eh,
            eh => transformer.HandlerProcessEnd -= eh,
            async () =>
            {
                var handler = new StallingHandler();
                handlerStatsBefore = TestTransformer.CapturePerformanceStats(TimeSpan.FromSeconds(0), 42, 0);
                handlerStatsAfter = TestTransformer.CapturePerformanceStats(TimeSpan.FromMilliseconds(300), 42, 42);
                var delta = handlerStatsBefore - handlerStatsAfter;
                //transformer.AppendHandlerDelta(delta);

                await transformer.NotifyHandlerEndAsync(
                    handlerName: handler.Name,
                    handlerIndex: 0,
                    duration: TimeSpan.FromMilliseconds(300),
                    bytesProcessed: 42,
                    exception: null,
                    statsBefore: handlerStatsBefore,
                    statsAfter: handlerStatsAfter
                );
            }
        );

        var transformEndEvent = await Assert.RaisesAnyAsync<TransformEndEventArgs>(
            eh => transformer.TransformProcessEnd += eh,
            eh => transformer.TransformProcessEnd -= eh,
            async () => await transformer.NotifyTransformEndAsync(
                    totalDuration: TimeSpan.FromMilliseconds(310),
                    totalBytesIn: 42,
                    totalBytesOut: 42,
                    exception: null
                ));

        var statDeltas = transformer.GetHandlerDeltas();

        // Assert
        Assert.NotNull(transformBeginEvent);
        Assert.Equal(0, transformBeginEvent.Arguments.InputLength);

        Assert.NotNull(handlerBeginEvent);
        Assert.Equal(nameof(StallingHandler), handlerBeginEvent.Arguments.HandlerName);
        Assert.Equal(0, handlerBeginEvent.Arguments.HandlerIndex);

        Assert.NotNull(handlerEndEvent);
        Assert.Equal(nameof(StallingHandler), handlerEndEvent.Arguments.HandlerName);
        Assert.Equal(0, handlerEndEvent.Arguments.HandlerIndex);
        Assert.Equal(42, handlerEndEvent.Arguments.BytesProcessed);
        Assert.Equal(TimeSpan.FromMilliseconds(300), handlerEndEvent.Arguments.Duration);
        Assert.True(handlerEndEvent.Arguments.Success);
        Assert.True(handlerEndEvent.Arguments.HasStats);
        Assert.Null(handlerEndEvent.Arguments.LastException);
        Assert.Equal(handlerStatsBefore, handlerEndEvent.Arguments.Stats?.StatsBefore);
        Assert.Equal(handlerStatsAfter, handlerEndEvent.Arguments.Stats?.StatsAfter);
        Assert.NotNull(handlerEndEvent.Arguments.Stats?.StatsDelta);

        Assert.NotNull(transformEndEvent);
        Assert.Equal(42, transformEndEvent.Arguments.TotalBytesIn);
        Assert.Equal(42, transformEndEvent.Arguments.TotalBytesOut);
        Assert.Equal(TimeSpan.FromMilliseconds(310), transformEndEvent.Arguments.TotalDuration);
        Assert.True(transformEndEvent.Arguments.HasStats);
        Assert.True(transformEndEvent.Arguments.Success);
        Assert.Null(transformEndEvent.Arguments.LastException);
        Assert.Equal(42, transformEndEvent.Arguments.Stats?.Stats?.BytesIn);
        Assert.Equal(42, transformEndEvent.Arguments.Stats?.Stats?.BytesOut);
        Assert.NotNull(transformEndEvent.Arguments.Stats?.HandlerStatsDeltas);
        Assert.InRange(transformEndEvent.Arguments.Stats.HandlerStatsDeltas.Count, 1, 1);
        Assert.NotNull(statDeltas);
        Assert.Empty(statDeltas);
    }

    [Fact]
    public async Task Transformer_Collects_Performance_Statistics()
    {
        // Assemble
        string messagePrefix = "got more soul than";
        string messageSuffix = " a sock with a hole";

        byte[] messagePrefixBytes = Encoding.UTF8.GetBytes(messagePrefix);
        byte[] messageSuffixBytes = Encoding.UTF8.GetBytes(messageSuffix);

        int messageByteLength = messagePrefixBytes.Length + messageSuffixBytes.Length;

        var options = new PassThruOptions(completionMessage: messageSuffix);

        TestTransformer transformer = new TransformBuilder()
                .ProcessWith<PassThruHandler, PassThruOptions>(new PassThruOptions() { CompletionMessage = messageSuffix })
                .Build<TestTransformer>();

        bool handlerEndEventHasStats = false,
             transformEndEventHasStats = false;

        long handlerBytesProcessed = -1,
             transformBytesIn = -1,
             transformBytesOut = -1;

        HandlerStatsSummary? handlerStatsSummary = null;
        TransformStatsSummary? transformStatsSummary = null;
        HandlerStatsAggregate? transformStatsAggregate = null;
        List<TransformStatsDelta>? transformStatsDeltas = null;
        

        // Use events for stats capture (for hi-perf envs we should use Channel<TransformEventArgs> MetricsChannel)
        transformer.HandlerProcessEnd += (o, e) =>
        {
            handlerEndEventHasStats = e.HasStats;
            handlerStatsSummary = e.Stats;
            handlerBytesProcessed = e.BytesProcessed;
        };

        transformer.TransformProcessEnd += (o, e) =>
        {
            transformEndEventHasStats = e.HasStats;
            transformStatsSummary = e.Stats;
            transformBytesIn = e.TotalBytesIn;
            transformBytesOut = e.TotalBytesOut;
            transformStatsAggregate = e.Stats?.HandlerStatsAggregate;
            transformStatsDeltas = e.Stats?.HandlerStatsDeltas;
        };

        using var input = new MemoryStream(messagePrefixBytes);
        using var output = new MemoryStream();

        // Act
        await transformer.ProcessAsync(input, output);
        output.Position = 0;

        using var sr = new StreamReader(output);
        var result = await sr.ReadToEndAsync(TestContext.Current.CancellationToken);

        // Assert
        // We've collected statistics data
        Assert.All([handlerEndEventHasStats, transformEndEventHasStats], (v) => Assert.True(v));
        Assert.All<object?>([handlerStatsSummary, transformStatsSummary, transformStatsAggregate, transformStatsDeltas], (v) => Assert.NotNull(v));

        // Ensure handler stats deltas cleared after operation
        Assert.Empty(transformer.GetHandlerDeltas()!);

        // Byte Count is correct
        Assert.Equal(messageByteLength, handlerBytesProcessed);
        Assert.Equal(messagePrefixBytes.Length, transformBytesIn);
        Assert.Equal(output.Length, transformBytesOut);
    }

    [Theory]
    [InlineData(3_670_016, HandlerStrategy.Buffered, Label = "3.5MB (Buffered)")]
    [InlineData(7_340_032, HandlerStrategy.Pipelined, Label = "7MB (Pipelined)")]
    public async Task Transform_With_Auto_Strategy_With_Threshold(int byteSize, HandlerStrategy expectedStrategy)
    {
        // Assemble
        var data = RandomNumberGenerator.GetBytes(byteSize);

        TestTransformer transformer = new TransformBuilder()
                .ProcessWith<PassThruHandler>()
                .Build<TestTransformer>();

        using var input = new MemoryStream(data);
        using var output = new MemoryStream();

        // Act
        await transformer.ProcessAsync(input, output);

        // Assert
        Assert.Equal(expectedStrategy, transformer.GetHandlerStrategy());
    }

    [Fact]
    public async Task Handler_Failure_Throws_Exception()
    {
        // Assemble
        var data = RandomNumberGenerator.GetBytes(3_670_016);

        TestTransformer transformer = new TransformBuilder()
                .ProcessWith<ExceptionalHandler>()
                .ProcessWith<PassThruHandler>()
                .Build<TestTransformer>();

        bool? handlerEndEventSuccessStatus = null,
              transformEndEventSuccessStatus = null;

        Exception? handlerEndEventLastException = null,
                   transformEndEventLastException = null;

        transformer.HandlerProcessEnd += (o, e) =>
        {
            handlerEndEventSuccessStatus = e.Success;
            handlerEndEventLastException = e.LastException;
        };

        transformer.TransformProcessEnd += (o, e) =>
        {
            transformEndEventSuccessStatus = e.Success;
            transformEndEventLastException = e.LastException;
        };

        // Act
        async Task Act()
        {
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();

            await transformer.ProcessAsync(input, output);
        }
        // Assert
        NotImplementedException ex = await Assert.ThrowsAsync<NotImplementedException>(Act);
        Assert.Equal($"This exception brought to you by: {nameof(ExceptionalHandler)}!", ex.Message);
        Assert.NotNull(handlerEndEventLastException);
        Assert.NotNull(transformEndEventLastException);
        Assert.All([handlerEndEventSuccessStatus, transformEndEventSuccessStatus], (v) => Assert.False(v));
        Assert.All([handlerEndEventLastException, transformEndEventLastException], (e) => Assert.Equal(ex, e));
    }

    [Theory]
    [InlineData(3_670_016, typeof(TransformBeginEventArgs), Label = "TransformBegin Event; 3.5MB (Buffered)")]
    [InlineData(7_340_032, typeof(TransformBeginEventArgs), Label = "TransformBegin Event; 7MB (Pipelined)")]
    [InlineData(3_670_016, typeof(HandlerBeginEventArgs), Label = "HandlerBegin Event; 3.5MB (Buffered)")]
    [InlineData(7_340_032, typeof(HandlerBeginEventArgs), Label = "HandlerBegin Event; 7MB (Pipelined)")]
    [InlineData(3_670_016, typeof(HandlerEndEventArgs), Label = "HandlerEnd Event; 3.5MB (Buffered)")]
    [InlineData(7_340_032, typeof(HandlerEndEventArgs), Label = "HandlerEnd Event; 7MB (Pipelined)")]
    [InlineData(3_670_016, typeof(TransformEndEventArgs), Label = "TransformEnd Event; 3.5MB (Buffered)")]
    [InlineData(7_340_032, typeof(TransformEndEventArgs), Label = "TransformEnd Event; 7MB (Pipelined)")]
    public async Task Bad_Event_Listener_Exception_Does_Not_Interrupt_Processing(int byteSize, Type EventArgType)
    {
        // Assemble
        var data = RandomNumberGenerator.GetBytes(byteSize);

        TestTransformBuilder builder = new();
        builder.ProcessWith<PassThruHandler>();

        switch (EventArgType)
        {
            case var t when t == typeof(TransformBeginEventArgs):
                builder.AddTransformBeginEventListener((s, e) 
                    => throw new NotImplementedException("This event has not been correctly implemented."));
                break;
            case var t when t == typeof(HandlerBeginEventArgs):
                builder.AddHandlerBeginEventListener((s, e) 
                    => throw new NotImplementedException("This event has not been correctly implemented."));
                break;
            case var t when t == typeof(HandlerEndEventArgs):
                builder.AddHandlerEndEventListener((s, e) 
                    => throw new NotImplementedException("This event has not been correctly implemented."));
                break;
            case var t when t == typeof(TransformEndEventArgs):
                builder.AddTransformEndEventListener((s, e) 
                    => throw new NotImplementedException("This event has not been correctly implemented."));
                break;
        }

        TestTransformer transformer = builder.Build<TestTransformer>();

        using var input = new MemoryStream(data);
        using var output = new MemoryStream();

        Exception? eventHandlerError = null;

        // Act
        try
        {
            await transformer.ProcessAsync(input, output);
        } catch (Exception ex)
        {
            eventHandlerError = ex;
        }

        // Assert
        Assert.Null(eventHandlerError);
    }
}
