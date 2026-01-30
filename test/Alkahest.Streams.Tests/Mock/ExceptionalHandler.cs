namespace Alkahest.Streams.Tests.Mock;

public class ExceptionalHandler : ITransformHandler
{
    public string Name { get; init; } = nameof(ExceptionalHandler);

    public IHandlerOptions Options { get; set; } = HandlerOptions.Empty;

    public async Task ProcessAsync(Stream input, Stream output)
    {
        throw new NotImplementedException($"This exception brought to you by: {nameof(ExceptionalHandler)}!");
    }
}
