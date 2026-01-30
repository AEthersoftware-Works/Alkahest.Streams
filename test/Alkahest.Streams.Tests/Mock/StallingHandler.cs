namespace Alkahest.Streams.Tests.Mock;

public class StallOptions
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

public class StallingHandler : ITransformHandler
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
