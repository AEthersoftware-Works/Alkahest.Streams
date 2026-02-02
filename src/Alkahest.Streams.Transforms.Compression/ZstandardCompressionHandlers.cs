using System.IO.Compression;

using NativeCompressions;

namespace Alkahest.Streams.Transforms.Compression;

/// <summary>
/// A handler for compressing streams using Brotli compression.
/// </summary>
public class ZstandardCompressionHandler : CompressionHandler
{
    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    public override string Name { get; init; } = nameof(ZstandardCompressionHandler);

    private static readonly ZstandardCompressionOptions DefaultOptions = ZstandardCompressionOptions.Default;

    /// <summary>
    /// Gets or sets the options for Zstandard compression.
    /// </summary>
    public new IHandlerOptions<ZstandardCompressionOptions> Options
    {
        get => (IHandlerOptions<ZstandardCompressionOptions>)base.Options;
        set => base.Options = value;
    }

    /// <summary>
    /// Processes the input stream by compressing it and writing to the output stream.
    /// </summary>
    /// <param name="input">The input stream to compress.</param>
    /// <param name="output">The output stream to write compressed data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        using var compressor = new ZstandardStream(output, Options.Value, true);
        await input.CopyToAsync(compressor);
    }
}

/// <summary>
/// A handler for decompressing Brotli-compressed streams.
/// </summary>
public class ZStandardDecompressionHandler : DecompressionHandler
{
    /// <summary>
    /// Gets the name of the handler.
    /// </summary>
    public override string Name { get; init; } = nameof(ZStandardDecompressionHandler);

    private static readonly ZstandardDecompressionOptions DefaultOptions = ZstandardDecompressionOptions.Default;

    /// <summary>
    /// Gets or sets the options for Zstandard compression.
    /// </summary>
    public new IHandlerOptions<ZstandardDecompressionOptions> Options
    {
        get => (IHandlerOptions<ZstandardDecompressionOptions>)base.Options;
        set => base.Options = value;
    }

    /// <summary>
    /// Processes the input stream by decompressing it and writing to the output stream.
    /// </summary>
    /// <param name="input">The input stream to decompress.</param>
    /// <param name="output">The output stream to write decompressed data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task ProcessAsync(Stream input, Stream output)
    {
        using var decompressor = new ZstandardStream(input, Options.Value, true);
        await decompressor.CopyToAsync(output);
    }
}
