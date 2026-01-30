using System.Text;

namespace Alkahest.Streams.Tests.Mock;

public struct PassThruOptions(string completionMessage = "")
{
    public PassThruOptions() : this(string.Empty) { }

    public string CompletionMessage = completionMessage;
}

public class PassThruHandler : ITransformHandler
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
