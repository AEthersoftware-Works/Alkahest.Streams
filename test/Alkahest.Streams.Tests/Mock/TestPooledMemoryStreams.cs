namespace Alkahest.Streams.Tests.Mock;

internal class TestPooledMemoryStream : PooledMemoryStream
{

}
internal class ThrowingWritePooledMemoryStream : PooledMemoryStream
{
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new OperationCanceledException(new CancellationToken(true));
    }
}

internal class ThrowingReadPooledMemoryStream : PooledMemoryStream
{
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new OperationCanceledException(new CancellationToken(true));
    }
}

internal class ThrowingWriteSpanPooledMemoryStream : PooledMemoryStream
{
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new OperationCanceledException(new CancellationToken(true));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new OperationCanceledException(new CancellationToken(true));
    }
}
