using System.Security.Cryptography;
using System.Text;

namespace Alkahest.Streams.Tests;

public class StreamTests
{
    private static PooledMemoryStream GetStream(int capacity = 65536 * 4) => new(capacity);

    [Fact]
    public void PooledMemoryStream_Initial_Length_ShouldBeZero()
    {
        // Assemble
        var stream = GetStream(65536 * 4);

        // Assert
        Assert.Equal(0, stream.Length);
        Assert.True(stream.Capacity >= (65536 * 4));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    [InlineData(65536)]
    [InlineData(3_670_016)]
    public void PooledMemoryStream_Writes_And_Reads_Correctly_Grows_Beyond_Initial_Capacity(int writeSize)
    {
        // Assemble
        var stream = GetStream(65536); // small initial
        var data = RandomNumberGenerator.GetBytes(writeSize);
        byte[] read = [];
        int readBytes = -1;

        // Act
        stream.Write(data, 0, data.Length);

        stream.Position = 0;
        
        read = new byte[writeSize];
        readBytes = stream.Read(read, 0, writeSize);

        // Assert
        Assert.Equal(writeSize, stream.Length);
        Assert.True(stream.Capacity >= writeSize);
        Assert.Equal(writeSize, readBytes);
        Assert.Equal(data, read);
    }

    [Fact]
    public async Task PooledMemoryStream_ToArray_Returns_Only_Written_Data()
    {
        // Assemble
        var stream = GetStream(65536);
        var buffer = "A"u8.ToArray();

        // Act
        await stream.WriteAsync(buffer, TestContext.Current.CancellationToken);

        // Assert
        var bytes = stream.ToArray();
        Assert.Equal(buffer, bytes);
    }

    [Fact]
    public void PooledMemoryStream_Dispose_ReturnsBufferToPool()
    {
        // Assemble
        var stream = GetStream(65536);

        // Act
        stream.WriteByte(42);
        stream.Dispose();

        // Assert
        // Can't test pool return directly, but no exception on dispose
        Assert.Throws<ObjectDisposedException>(() => stream.WriteByte(0));
    }

    [Fact]
    public async Task TrackingStreamWriter_Counts_Bytes_On_Write()
    {
        // Assemble
        var inner = new MemoryStream();
        var tracking = new TrackingStreamWriter(inner);

        var data = new byte[] { 1, 2, 3 };

        // Act
        await tracking.WriteAsync(data, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, tracking.BytesWritten);
    }

    [Fact]
    public async Task TrackingStreamWriter_Counts_Copied_Bytes()
    {
        // Assemble
        var inner = new MemoryStream();
        var tracking = new TrackingStreamWriter(inner);

        var source = new MemoryStream([4, 5, 6, 7]);

        // Act
        await source.CopyToAsync(tracking, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(4, tracking.BytesWritten);
    }

    [Fact]
    public async Task TrackingStreamWriter_Handles_Partial_Writes()
    {
        // Assemble
        var inner = new MemoryStream();
        var tracking = new TrackingStreamWriter(inner);

        // Act
        tracking.Write([1, 2, 3], 1, 2); // write 2,3

        // Assert
        Assert.Equal(2, tracking.BytesWritten);
    }
}
