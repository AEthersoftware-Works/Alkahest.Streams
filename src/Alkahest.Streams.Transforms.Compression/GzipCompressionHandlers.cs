using System.IO.Compression;

namespace Alkahest.Streams.Transforms.Compression;


#if NET8_0
/// <summary>
/// Handler for compressing streams using GZip.
/// </summary>
public class GzipCompressionHandler : CompressionHandler
{
    /// <inheritdoc/>
    public override string Name { get; init; } = nameof(GzipCompressionHandler);

    /// <inheritdoc/>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        using var compressor = new GZipStream(output, CompressionLevel.Optimal, true);
        await input.CopyToAsync(compressor);
    }
}
#elif NET10_0_OR_GREATER
/// <summary>
/// Handler for compressing streams using GZip.
/// </summary>
public class GzipCompressionHandler : CompressionHandler
{
    /// <inheritdoc/>
    public override string Name { get; init; } = nameof(GzipCompressionHandler);

    private static readonly ZLibCompressionOptions DefaultOptions = new()
    {
        CompressionLevel = -1,
        CompressionStrategy = ZLibCompressionStrategy.Default
    };

    /// <summary>
    /// Gets or sets the options for GZip compression.
    /// </summary>
    public new IHandlerOptions<ZLibCompressionOptions> Options
    {
        get => (IHandlerOptions<ZLibCompressionOptions>)base.Options;
        set => base.Options = value;
    }

    /// <summary>
    /// Initializes a new instance of the GzipCompressionHandler with default options.
    /// </summary>
    public GzipCompressionHandler() => Options = HandlerOptions.Create(DefaultOptions);

    /// <inheritdoc/>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        using var compressor = new GZipStream(output, Options.Value ?? DefaultOptions, true);
        await input.CopyToAsync(compressor);
    }
}
#endif

/// <summary>
/// Handler for decompressing GZip-compressed streams.
/// </summary>
public class GzipDecompressionHandler : DecompressionHandler
{
    /// <inheritdoc/>
    public override string Name { get; init; } = nameof(GzipDecompressionHandler);

    /// <inheritdoc/>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        using var decompressor = new GZipStream(input, CompressionMode.Decompress, true);
        await decompressor.CopyToAsync(output);
    }
}
