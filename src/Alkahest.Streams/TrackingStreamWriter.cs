namespace Alkahest.Streams;

/// <summary>
/// A write-only stream wrapper that tracks bytes written to the underlying stream.
/// </summary>
/// <param name="stream">A writable stream.</param>
public class TrackingStreamWriter(Stream stream) : Stream
{
    private readonly Stream _inner = stream;

    /// <summary>
    /// The bytes written to the stream.
    /// </summary>
    public long BytesWritten { get; private set; }

    /// <inheritdoc/>
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        await _inner.WriteAsync(buffer, offset, count, ct);
        BytesWritten += count;
    }

    /// <inheritdoc/>
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _inner.WriteAsync(buffer, cancellationToken);
        BytesWritten += buffer.Length;
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        _inner.Write(buffer, offset, count);
        BytesWritten += count;
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        _inner.WriteByte(value);
        BytesWritten += 1;
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _inner.Write(buffer);
        BytesWritten += buffer.Length;
    }

    /// <inheritdoc/>
    public override void Flush() => _inner.Flush();

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _inner.FlushAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override bool CanRead => _inner.CanRead;

    /// <inheritdoc/>
    public override bool CanSeek => _inner.CanSeek;

    /// <inheritdoc/>
    public override bool CanWrite => _inner.CanWrite;

    /// <inheritdoc/>
    public override long Length => _inner.Length;

    /// <inheritdoc/>
    public override long Position { get => _inner.Position; set => _inner.Position = value; }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    /// <inheritdoc/>
    public override void SetLength(long value) => _inner.SetLength(value);
}
