using System.Buffers;
using System.IO.Pipelines;

namespace Alkahest.Streams;

/// <summary>
/// A Stream implementation that wraps a PipeReader, allowing asynchronous reading from the pipe.
/// </summary>
public sealed class PipeReaderAsStream(PipeReader reader) : Stream
{
    private readonly PipeReader _reader = reader;

    /// <inheritdoc/>
    public override bool CanRead => true;
    /// <inheritdoc/>
    public override bool CanSeek => false;
    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Synchronous Read not supported. Use ReadAsync.");
    }

    /// <summary>
    /// Asynchronously reads a sequence of bytes from the pipe into the given buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read data into.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of bytes read, or 0 if the end of the stream is reached.</returns>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            ReadResult result = await _reader.ReadAsync(cancellationToken);

            if (result.IsCanceled)
                throw new OperationCanceledException();

            if (result.Buffer.IsEmpty)
            {
                if (result.IsCompleted)
                    return 0; // EOF
                continue; // Wait for more data
            }

            long bytesToCopy = Math.Min(result.Buffer.Length, buffer.Length);
            result.Buffer.Slice(0, bytesToCopy).CopyTo(buffer.Span[..(int)bytesToCopy]);

            _reader.AdvanceTo(result.Buffer.GetPosition(bytesToCopy));

            return (int)bytesToCopy;
        }
    }

    // public override int Read(byte[] buffer, int offset, int count)
    // {
    //     var buff = buffer.AsMemory();
    //     _reader.TryRead(out ReadResult readResult);
    //     readResult.Buffer.Slice(0, Math.Min(readResult.Buffer.Length, buff.Length)).CopyTo(buff.Span);
    //     var bytesRead = buff.Length;
    //     if (readResult.IsCompleted && bytesRead == 0) return 0;
    //     return (int)bytesRead;
    // }

    // public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    // {
    //     var result = await _reader.ReadAsync(cancellationToken);
    //     result.Buffer.Slice(0, Math.Min(result.Buffer.Length, buffer.Length)).CopyTo(buffer.Span);
    //     var bytesRead = buffer.Length;
    //     _reader.AdvanceTo(result.Buffer.GetPosition(bytesRead));
    //     if (result.IsCompleted && bytesRead == 0) return 0;
    //     return (int)bytesRead;
    // }

    // Other overrides: Length/Position/Seek/Write/Flush throw NotSupportedException
    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc/>
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void Flush() => throw new NotImplementedException();
}

