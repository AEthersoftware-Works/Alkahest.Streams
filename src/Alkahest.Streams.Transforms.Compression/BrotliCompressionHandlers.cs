using System.IO.Compression;

namespace Alkahest.Streams.Transforms.Compression;

#if NET8_0
/// <summary>
/// A handler for compressing streams using Brotli compression.
/// </summary>
public class BrotliCompressionHandler : CompressionHandler
{
    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    public override string Name { get; init; } = nameof(BrotliCompressionHandler);
    
    /// <summary>
    /// Processes the input stream by compressing it and writing to the output stream.
    /// </summary>
    /// <param name="input">The input stream to compress.</param>
    /// <param name="output">The output stream to write compressed data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        using var compressor = new BrotliStream(output, CompressionLevel.Optimal, true);
        await input.CopyToAsync(compressor);
    }
}
#elif NET10_0_OR_GREATER
/// <summary>
/// A handler for compressing streams using Brotli compression.
/// </summary>
public class BrotliCompressionHandler : CompressionHandler
{
    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    public override string Name { get; init; } = nameof(BrotliCompressionHandler);

    private static readonly BrotliCompressionOptions DefaultOptions = new() { Quality = 4 };

    /// <summary>
    /// Gets or sets the options for Brotli compression.
    /// </summary>
    public new IHandlerOptions<BrotliCompressionOptions> Options
    {
        get => (IHandlerOptions<BrotliCompressionOptions>)base.Options;
        set => base.Options = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BrotliCompressionHandler"/> class.
    /// </summary>
    public BrotliCompressionHandler() => Options = HandlerOptions.Create(DefaultOptions);

    /// <summary>
    /// Processes the input stream by compressing it and writing to the output stream.
    /// </summary>
    /// <param name="input">The input stream to compress.</param>
    /// <param name="output">The output stream to write compressed data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        using var compressor = new BrotliStream(output, Options.Value ?? DefaultOptions, true);
        await input.CopyToAsync(compressor);
    }
}
#endif

/// <summary>
/// A handler for decompressing Brotli-compressed streams.
/// </summary>
public class BrotliDecompressionHandler : DecompressionHandler
{
    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    public override string Name { get; init; } = nameof(BrotliDecompressionHandler);

    /// <summary>
    /// Processes the input stream by decompressing it and writing to the output stream.
    /// </summary>
    /// <param name="input">The input stream to decompress.</param>
    /// <param name="output">The output stream to write decompressed data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        using var decompressor = new BrotliStream(input, CompressionMode.Decompress, true);
        await decompressor.CopyToAsync(output);
    }
}
