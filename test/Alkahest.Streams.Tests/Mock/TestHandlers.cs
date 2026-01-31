using System;
using System.Text;

namespace Alkahest.Streams.Tests.Mock;

internal class ExceptionalHandler : ITransformHandler
{
    public string Name { get; init; } = nameof(ExceptionalHandler);

    public IHandlerOptions Options { get; set; } = HandlerOptions.Empty;

    public async Task ProcessAsync(Stream input, Stream output)
    {
        throw new NotImplementedException($"This exception brought to you by: {nameof(ExceptionalHandler)}!");
    }
}

internal struct PassThruOptions(string completionMessage = "")
{
    public PassThruOptions() : this(string.Empty) { }

    public string CompletionMessage = completionMessage;
}

internal class PassThruHandler : ITransformHandler
{
    /// <inheritdoc/>
    public string Name { get; init; } = nameof(PassThruHandler);

    private static readonly PassThruOptions DefaultOptions = new()
    {
        CompletionMessage = "World!",
    };

    /// <summary>
    /// Gets or sets the options for image reading, primarily the expected input format.
    /// </summary>
    public IHandlerOptions<PassThruOptions> Options { get; set; }

    IHandlerOptions ITransformHandler.Options
    {
        get => Options;
        set => Options = (IHandlerOptions<PassThruOptions>)value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PassThruHandler"/> class.
    /// </summary>
    public PassThruHandler() => Options = HandlerOptions.Create(DefaultOptions);

    /// <summary>
    /// Passes through the input stream to the output.
    /// </summary>
    /// <param name="input">The input stream.</param>
    /// <param name="output">The output stream.</param>
    public async Task ProcessAsync(Stream input, Stream output)
    {
        // Copy the stream
        await input.CopyToAsync(output);

        // Append the message
        if (!string.IsNullOrWhiteSpace(Options.Value.CompletionMessage))
        {
            var messageBytes = Encoding.UTF8.GetBytes(Options.Value.CompletionMessage);
            await output.WriteAsync(messageBytes);
        }
    }
}

internal class StallOptions
{
#if NET8_0
    public Guid Id { get; set; } = Guid.NewGuid();
#endif
#if NET10_0_OR_GREATER
    public Guid Id { get; set; } = Guid.CreateVersion7();
#endif
    public TimeSpan Delay { get; set; }

    public static StallOptions Default { get; } = new() { Id = Guid.Empty, Delay = TimeSpan.FromSeconds(5) };
}

internal class StallingHandler : ITransformHandler
{
    public string Name { get; init; } = nameof(StallingHandler);

    private static readonly StallOptions DefaultOptions = StallOptions.Default;

    /// <summary>
    /// Gets or sets the options for image reading, primarily the expected input format.
    /// </summary>
    public IHandlerOptions<StallOptions> Options { get; set; }

    IHandlerOptions ITransformHandler.Options
    {
        get => Options;
        set => Options = (IHandlerOptions<StallOptions>)value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StallingHandler"/> class.
    /// </summary>
    public StallingHandler() => Options = HandlerOptions.Create(DefaultOptions);

    public async Task ProcessAsync(Stream input, Stream output)
    {
        // Wait for configured time...
        await Task.Delay(Options.Value?.Delay ?? TimeSpan.FromSeconds(10));

        // Copy the stream
        await input.CopyToAsync(output);
    }
}
