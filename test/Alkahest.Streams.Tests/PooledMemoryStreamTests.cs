using System.Security.Cryptography;

using Alkahest.Streams.Tests.Mock;

using Testably.Abstractions.Testing;

namespace Alkahest.Streams.Tests;

public partial class PooledMemoryStreamTests
{
    private static PooledMemoryStream GetStream(int capacity = 65536 * 4) => new(capacity);

    [Fact]
    public void Initial_Length_ShouldBeZero()
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
    public void Writes_And_Reads_Correctly_Grows_Beyond_Initial_Capacity(int writeSize)
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
    public async Task ToArray_Returns_Only_Written_Data()
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
    public void Dispose_ReturnsBufferToPool()
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
    public void Constructor_With_Buffer()
    {
        // Assemble
        var buffer = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var stream = new PooledMemoryStream(buffer);

        // Assert
        Assert.Equal(5, stream.Length);
        Assert.InRange(stream.Capacity, 5, 16);
        Assert.True(stream.CanRead);
        Assert.True(stream.CanWrite);
    }

    [Fact]
    public void Read_Works()
    {
        // Assemble
        var stream = GetStream();
        var writeData = new byte[] { 1, 2, 3, 4, 5 };
        stream.Write(writeData, 0, 5);
        stream.Position = 0;
        var readData = new byte[5];

        // Act
        var bytesRead = stream.Read(readData, 0, 5);

        // Assert
        Assert.Equal(5, bytesRead);
        Assert.Equal(writeData, readData);
    }

    [Fact]
    public void Seek_Works()
    {
        // Assemble
        var stream = GetStream();
        stream.WriteByte(1);
        stream.WriteByte(2);
        stream.WriteByte(3);
        bool canSeek = stream.CanSeek;

        // Act
        stream.Seek(1, SeekOrigin.Begin);

        // Assert
        Assert.True(canSeek);
        Assert.Equal(1, stream.Position);
        Assert.Equal(2, stream.ReadByte());
    }

    [Fact]
    public void InternalReadSpan_Works()
    {
        // InternalReadSpan
        // Assemble
        var stream = GetStream();
        var writeData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        stream.Write(writeData, 0, 5);
        stream.Position = 0;
        ReadOnlySpan<byte> data = stream.InternalReadSpan(writeData.Length);

        // Assert
        Assert.Equal(5, data.Length);
    }

    [Fact]
    public void InternalReadSpan_Throws_EndOfStream_Exception()
    {
        // Assemble
        var stream = GetStream();
        var writeData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var act = () =>
        {
            stream.Write(writeData, 0, 5);
            stream.Position = 0;
            ReadOnlySpan<byte> data = stream.InternalReadSpan(writeData.Length * 2);
        };

        // Assert
        Assert.ThrowsAny<EndOfStreamException>(act);
    }

    [Fact]
    public void InternalGetBuffer_Works()
    {
        // Assemble
        var stream = GetStream();
        var writeData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        stream.Write(writeData, 0, 5);
        var buffer = stream.InternalGetBuffer();

        // Assert
        Assert.NotNull(buffer);
        Assert.NotEmpty(buffer);
    }

    [Fact]
    public void InternalGetPosition_Works()
    {
        // Assemble
        var stream = GetStream();
        var writeData = new byte[] { 1, 2, 3, 4, 5 };
        int? position = null;

        // Act
        stream.Write(writeData, 0, 5);
        position = stream.InternalGetPosition();

        // Assert
        Assert.NotNull(position);
        Assert.Equal(stream.Position, (long)position);
    }

    [Fact]
    public void InternalEmulateRead_Works()
    {
        // Assemble
        var stream = GetStream();
        var writeData = new byte[] { 1, 2, 3, 4, 5 };
        int? read = null;

        // Act
        stream.Write(writeData, 0, 5);
        stream.Position = 0;
        read = stream.InternalEmulateRead(5);

        // Assert
        Assert.NotNull(read);
        Assert.Equal(writeData.Length, read);
    }

    [Fact]
    public void SetLength_Works()
    {
        // Assemble
        var stream = GetStream();
        stream.Write(new byte[10], 0, 10);

        // Act
        stream.SetLength(5);

        // Assert
        Assert.Equal(5, stream.Length);
        Assert.Equal(5, stream.Position);
    }

    [Fact]
    public void Capacity_Increases()
    {
        // Assemble
        var stream = GetStream(10);

        // Act
        stream.Capacity = 20;

        // Assert
        Assert.Equal(20, stream.Capacity);
    }

    [Fact]
    public void Capacity_Decreases()
    {
        // Assemble
        var stream = GetStream(32);

        // Act
        int oldCapacity = stream.Capacity;
        stream.Capacity = 0;

        // Assert
        // TODO: May need to revisit this. 
        // By default the min ArrayPool<byte> rented min size is 16
        // If not handled properly it may append zero'd bytes into the result.
        Assert.Equal(0, stream.Capacity);
        Assert.True(stream.Capacity < oldCapacity);
    }

    [Fact]
    public void GetBuffer_Works()
    {
        // Assemble
        var stream = GetStream();
        stream.WriteByte(42);

        // Act
        var buffer = stream.GetBuffer();

        // Assert
        Assert.Equal(42, buffer[0]);
    }

    [Fact]
    public void TryGetBuffer_Works()
    {
        // Assemble
        var stream = GetStream();

        // Act
        stream.WriteByte(42);
        var success = stream.TryGetBuffer(out var segment);

        // Assert
        Assert.True(success);
        Assert.Equal(42, segment[0]);
    }

    [Fact]
    public void TryGetBuffer_Returns_False_When_Stream_Not_Publicly_Visible()
    {
        // Assemble
        byte[] buffer = [1, 2, 3];
        var stream = new PooledMemoryStream(buffer, 0, buffer.Length, true, false);

        // Act
        var success = stream.TryGetBuffer(out var segment);

        // Assert
        Assert.False(success);
        Assert.Equal(0, segment.Count);
    }

    [Fact]
    public void Constructor_Buffer_Null_Throws_ArgumentNull_Exception()
    {
        // Assemble

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        static PooledMemoryStream Constructor1() => new(null, 0, 0);
        static PooledMemoryStream Constructor2() => new(null, 0, 0, true);
        static PooledMemoryStream Constructor3() => new(null, 0, 0, true, true);

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        Assert.Throws<ArgumentNullException>(Constructor1);
        Assert.Throws<ArgumentNullException>(Constructor2);
        Assert.Throws<ArgumentNullException>(Constructor3);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(-1, -1)]
    public void Constructor_Buffer_Negative_Index_Or_Count_Throws_ArgumentOutOfRange_Exception(int index, int count)
    {
        // Assemble
        byte[] buffer = [1, 2, 3];

        // Act
        var constructor1 = () => new PooledMemoryStream(buffer, index, count);
        var constructor2 = () => new PooledMemoryStream(buffer, index, count, true);
        var constructor3 = () => new PooledMemoryStream(buffer, index, count, true, true);

        // Assert 
        Assert.Throws<ArgumentOutOfRangeException>(constructor1);
        Assert.Throws<ArgumentOutOfRangeException>(constructor2);
        Assert.Throws<ArgumentOutOfRangeException>(constructor3);
    }

    [Theory]
    [InlineData(1, 3)] // index + count > buffer.Length
    [InlineData(3, 1)]
    public void Constructor_Buffer_OutOfBounds_Throws_Argument_Exception(int index, int count)
    {
        // Assemble
        byte[] buffer = [1, 2, 3];

        // Act
        var constructor1 = () => new PooledMemoryStream(buffer, index, count);
        var constructor2 = () => new PooledMemoryStream(buffer, index, count, true);
        var constructor3 = () => new PooledMemoryStream(buffer, index, count, true, true);

        Assert.Throws<ArgumentException>(constructor1);
        Assert.Throws<ArgumentException>(constructor2);
        Assert.Throws<ArgumentException>(constructor3);
    }

    [Fact]
    public void Constructor_Buffer_Empty_Buffer_Valid_Index_Count_Succeeds()
    {
        // Assemble
        byte[] buffer = [];

        // Act
        var stream = new PooledMemoryStream(buffer, 0, 0);

        // Assert
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void Constructor_Buffer_Index_Count_At_Boundary_Succeeds()
    {
        // Assemble
        byte[] buffer = [1, 2, 3];

        // Act
        var stream = new PooledMemoryStream(buffer, 0, 3);

        // Assert
        Assert.Equal(3, stream.Length);
    }

    [Fact]
    public void Dispose_Then_Write_Throws_ObjectDisposed_Exception()
    {
        // Assemble
        using var stream = GetStream();

        // Act
        stream.Dispose();
        var act = () => stream.WriteByte(1);

        // Assert
        Assert.Throws<ObjectDisposedException>(act);
    }

    [Fact]
    public void ReadOnly_Write_Throws_NotSupported_Exception()
    {
        // Assemble
        byte[] buffer = [1, 2, 3];

        // Act
        using var stream = new PooledMemoryStream(buffer, 0, 3, false);

        var act = () => stream.WriteByte(1);

        // Assert
        Assert.Throws<NotSupportedException>(act);
    }

    [Fact]
    public void GetBuffer_NonExposable_ThrowsUnauthorizedAccessException()
    {
        // Assemble
        var buffer = new byte[10];

        // Act
        var stream = new PooledMemoryStream(buffer, false);
        var act = () => stream.GetBuffer();

        // Assert
        Assert.Throws<UnauthorizedAccessException>(act);
    }

    [Fact]
    public void Read_Beyond_Length_Does_Not_Throw_EndOfStream_Exception()
    {
        // Assemble
        static byte[] ByteSequence(int count) => [.. Enumerable.Range(0, count).Select(x => (byte)x)];
        byte[] incrementalBytes = ByteSequence(16);

        // Act
        using var stream = GetStream(16);
        // Min capacity from the pool is 16.. Need bigger numbers to test
        stream.Write(incrementalBytes, 0, 16);
        stream.Position = 0;
        byte[] buffer = new byte[24];

        int bytesRead = stream.Read(buffer, 0, 24);

        // Assert
        Assert.Equal(incrementalBytes.Length, bytesRead);
        Assert.Equal(stream.Length, bytesRead);
        Assert.Equal(stream.Capacity, bytesRead);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue + 1L)]
    public void Position_Set_Negative_Or_Overflow_ThrowsArgumentOutOfRange_Exception(long position)
    {
        // Assemble

        // Act
        using var stream = GetStream();
        var setPosition = () => { stream.Position = position; };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(setPosition);
    }

    [Fact]
    public async Task ReadAsync_Cancellation_Before_Read_Returns_Canceled_Task()
    {
        // Assemble
        byte[] firstWriteBuffer = [1, 2, 3];
        byte[] testBuffer = new byte[3];

        // Act
        using var stream = GetStream();
        stream.Write(firstWriteBuffer, 0, 3);
        stream.Position = 0;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await stream.ReadAsync(testBuffer, 0, 3, cts.Token);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

    [Fact]
    public async Task ReadAsync_Memory_Cancellation_Before_Read_Returns_Canceled_ValueTask()
    {
        // Assemble
        byte[] firstWriteBuffer = [1, 2, 3];
        var buffer = new Memory<byte>(new byte[3]);

        // Act
        using var stream = GetStream();
        stream.Write(firstWriteBuffer, 0, 3);
        stream.Position = 0;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

#pragma warning disable CA2022 // Avoid inexact read with Stream.Read

        var act = async () => { await stream.ReadAsync(buffer, cts.Token); };

#pragma warning restore CS8625 // Avoid inexact read with Stream.Read

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

    [Fact]
    public void CopyTo_Null_Destination_Throws_ArgumentNull_Exception()
    {
        // Assemble
        Stream? nullStream = null;

        // Act
        using var stream = GetStream();

#pragma warning disable CS8604 // Possible null reference argument.

        var act = () => stream.CopyTo(nullStream, 1024);

#pragma warning restore CS8604 // Possible null reference argument.


        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void CopyTo_Invalid_Buffer_Size_Throws_ArgumentOutOfRange_Exception(int bufferSize)
    {
        // Assemble
        using var stream = GetStream();
        using var dest = new MemoryStream();

        // Act
        var act = () => { stream.CopyTo(dest, bufferSize); };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task CopyToAsync_CancellationBeforeCopy_ReturnsCanceledTask()
    {
        // Assemble
        byte[] firstWriteBuffer = [1, 2, 3];

        // Act
        using var stream = GetStream();
        stream.Write(firstWriteBuffer, 0, 3);
        stream.Position = 0;

        using var dest = new MemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await stream.CopyToAsync(dest, 1024, cts.Token);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

    [Fact]
    public void Seek_InvalidOffset_ThrowsArgumentOutOfRangeException()
    {
        // Assemble
        long offset = long.MaxValue;

        // Act
        using var stream = GetStream();
        var act = () => { stream.Seek(offset, SeekOrigin.Begin); };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Seek_BeforeOrigin_Throws_IO_Exception()
    {
        // Assemble

        // Act
        using var stream = GetStream();
        var act = () => { stream.Seek(-1, SeekOrigin.Begin); };

        // Assert
        Assert.Throws<IOException>(act);
    }

    [Fact]
    public void Write_Overflow_Throws_ArgumentOutOfRange_Exception()
    {
        // Assemble
        byte[] buffer = new byte[20];
        int length = Array.MaxLength;
        int position = length;

        // Act
        using var stream = GetStream();
        // Simulate overflow by setting position near max
        stream.SetLength(length);
        stream.Position = position;

        var act = () => { stream.Write(buffer, 0, 20); };

        // Assert
        // When stream attempts to resize, the range is beyond maximum.
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task WriteAsync_Cancellation_Before_Write_Returns_Canceled_Task()
    {
        // Assemble
        byte[] buffer = [1, 2, 3];

        // Act
        using var stream = GetStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => { await stream.WriteAsync(buffer, 0, 3, cts.Token); };

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

    [Fact]
    public async Task WriteAsync_Memory_Cancellation_Before_Write_Returns_Canceled_ValueTask()
    {
        // Assemble
        var buffer = new ReadOnlyMemory<byte>([1, 2, 3]);

        // Act
        using var stream = GetStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => { await stream.WriteAsync(buffer, cts.Token); };

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

    [Fact]
    public void WriteTo_Null_Stream_Throws_ArgumentNull_Exception()
    {
        // Assemble
        Stream? nullStream = null;

        // Act
        using var stream = GetStream();

#pragma warning disable CS8604 // Possible null reference argument.

        var act = () => stream.WriteTo(nullStream);

#pragma warning restore CS8604 // Possible null reference argument.

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public async Task FlushAsync_CancellationBeforeFlush_ReturnsCanceledTask()
    {
        // Assemble
        using var stream = GetStream();
        using var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();

        var act = async () => { await stream.FlushAsync(cts.Token); };

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

    [Fact]
    public void Read_Span_Works()
    {
        // Assemble
        var stream = GetStream();
        var writeData = new byte[] { 1, 2, 3, 4, 5 };
        stream.Write(writeData, 0, 5);
        stream.Position = 0;
        Span<byte> readBuffer = stackalloc byte[5];

        // Act
        var bytesRead = stream.Read(readBuffer);

        // Assert
        Assert.Equal(5, bytesRead);
        Assert.Equal(writeData, readBuffer.ToArray());
    }

    [Fact]
    public void Read_Span_Reads_Zero_When_At_End()
    {
        // Assemble
        var stream = GetStream();
        stream.WriteByte(1);
        stream.Position = 1;
        Span<byte> readBuffer = stackalloc byte[5];

        // Act
        var bytesRead = stream.Read(readBuffer);

        // Assert
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public void ReadByte_Works()
    {
        // Assemble
        var stream = GetStream();
        stream.WriteByte(42);

        // Act
        stream.Position = 0;
        var result = stream.ReadByte();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void ReadByte_Returns_Negative_One_At_End()
    {
        // Assemble
        var stream = GetStream();
        stream.WriteByte(1);
        stream.Position = 1;

        // Act
        var result = stream.ReadByte();

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Write_Span_Works()
    {
        // Assemble
        var stream = GetStream();
        ReadOnlySpan<byte> data = [1, 2, 3, 4, 5];

        // Act
        stream.Write(data);

        // Assert
        Assert.Equal(5, stream.Length);
        var buffer = stream.GetBuffer();
        Assert.Equal(data.ToArray(), buffer[..5]);
    }

    [Fact]
    public void WriteByte_Works()
    {
        // Assemble
        var stream = GetStream();

        // Act
        stream.WriteByte(42);

        // Assert
        Assert.Equal(1, stream.Length);
        Assert.Equal(42, stream.GetBuffer()[0]);
    }

    [Fact]
    public async Task ReadAsync_Memory_Works()
    {
        // Assemble
        var stream = GetStream();
        var writeData = new byte[] { 1, 2, 3, 4, 5 };
        stream.Write(writeData, 0, 5);
        stream.Position = 0;
        var readBuffer = new Memory<byte>(new byte[5]);

        // Act
        var bytesRead = await stream.ReadAsync(readBuffer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, bytesRead);
        Assert.Equal(writeData, readBuffer.ToArray());
    }

    [Fact]
    public async Task ReadAsync_Memory_Reads_Zero_When_At_End()
    {
        // Assemble
        var stream = GetStream();
        stream.WriteByte(1);
        stream.Position = 1;
        var readBuffer = new Memory<byte>(new byte[5]);

        // Act
        var bytesRead = await stream.ReadAsync(readBuffer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public async Task WriteAsync_Memory_Works()
    {
        // Assemble
        var stream = GetStream();
        var data = new ReadOnlyMemory<byte>([1, 2, 3, 4, 5]);

        // Act
        await stream.WriteAsync(data, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, stream.Length);
        var buffer = stream.GetBuffer();
        Assert.Equal(data.ToArray(), buffer[..5]);
    }

    [Fact]
    public async Task CopyToAsync_With_PooledMemoryStream_Destination_Works()
    {
        // Assemble
        var source = GetStream();
        source.Write([1, 2, 3, 4, 5], 0, 5);
        source.Position = 0;
        var destination = GetStream();

        // Act
        await source.CopyToAsync(destination, 1024, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, destination.Length);
        var destBuffer = destination.GetBuffer();
        Assert.Equal([1, 2, 3, 4, 5], destBuffer[..5]);
    }

    [Fact]
    public async Task CopyToAsync_With_Buffer_Size_Larger_Than_Stream_Works()
    {
        // Assemble
        var source = GetStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 0;
        using var destination = new MemoryStream();

        // Act
        await source.CopyToAsync(destination, 10, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, destination.Length);
        destination.Position = 0;
        var result = new byte[3];
        destination.Read(result, 0, 3);
        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void WriteTo_Works()
    {
        // Assemble
        var stream = GetStream();
        stream.Write([1, 2, 3, 4, 5], 0, 5);
        using var destination = new MemoryStream();

        // Act
        stream.WriteTo(destination);

        // Assert
        Assert.Equal(5, destination.Length);
        destination.Position = 0;
        var result = new byte[5];
        destination.Read(result, 0, 5);
        Assert.Equal([1, 2, 3, 4, 5], result);
    }

    [Fact]
    public void ToArray_When_Length_Zero_Returns_Empty_Array()
    {
        // Assemble
        var stream = GetStream();

        // Act
        var result = stream.ToArray();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void TryGetBuffer_Returns_False_For_Non_Exposable_Stream()
    {
        // Assemble
        byte[] buffer = [1, 2, 3];
        var stream = new PooledMemoryStream(buffer, 0, 3, true, false);

        // Act
        var success = stream.TryGetBuffer(out var segment);

        // Assert
        Assert.False(success);
        Assert.Equal(0, segment.Count);
    }

    [Fact]
    public void Position_Set_To_Value_Causing_Overflow_Throws_ArgumentOutOfRangeException()
    {
        // Assemble
        var stream = GetStream();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = long.MaxValue);
    }

    [Fact]
    public void Seek_With_Invalid_SeekOrigin_Throws_ArgumentException()
    {
        // Assemble
        var stream = GetStream();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stream.Seek(0, (SeekOrigin)999));
    }

    [Fact]
    public void Write_With_Count_Causing_Overflow_Throws_IOException()
    {
        // Assemble
        var stream = GetStream();
        // Set length and position to near max to trigger overflow
        stream.SetLength(int.MaxValue - 100);
        stream.Position = int.MaxValue - 100;
        byte[] buffer = new byte[200];

        // Act & Assert
        Assert.Throws<IOException>(() => stream.Write(buffer, 0, 200));
    }

    [Fact]
    public void SetLength_With_Value_Causing_Overflow_Throws_ArgumentOutOfRangeException()
    {
        // Assemble
        var stream = GetStream();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetLength(long.MaxValue));
    }

    [Fact]
    public void Capacity_Set_To_Value_Less_Than_Length_Throws_ArgumentOutOfRangeException()
    {
        // Assemble
        var stream = GetStream();
        stream.Write(new byte[10], 0, 10);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Capacity = 5);
    }

    [Fact]
    public void InternalReadSpan_Reads_Correctly()
    {
        // Assemble
        var stream = GetStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        stream.Write(data, 0, 5);
        stream.Position = 0;

        // Act
        var span = stream.InternalReadSpan(3);

        // Assert
        Assert.Equal(3, span.Length);
        Assert.Equal([1, 2, 3], span.ToArray());
        Assert.Equal(3, stream.Position);
    }

    [Fact]
    public void InternalGetBuffer_Returns_Buffer()
    {
        // Assemble
        var stream = GetStream();
        stream.WriteByte(42);

        // Act
        var buffer = stream.InternalGetBuffer();

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= 1);
        Assert.Equal(42, buffer[0]);
    }

    [Fact]
    public void InternalGetPosition_Returns_Current_Position()
    {
        // Assemble
        var stream = GetStream();
        stream.WriteByte(1);
        stream.Position = 1;

        // Act
        var position = stream.InternalGetPosition();

        // Assert
        Assert.Equal(1, position);
    }

    [Fact]
    public void InternalEmulateRead_Advances_Position()
    {
        // Assemble
        var stream = GetStream();
        stream.Write(new byte[10], 0, 10);
        stream.Position = 0;

        // Act
        var result = stream.InternalEmulateRead(5);

        // Assert
        Assert.Equal(5, result);
        Assert.Equal(5, stream.Position);
    }

    [Fact]
    public async Task CachedCompletedInt32Task_Returns_Correct_Task()
    {
        // Assemble
        var cache = new CachedCompletedInt32Task();

        // Act
        var task = cache.GetTask(42);

        // Assert
        Assert.True(task.IsCompleted);
        Assert.Equal(42, await task);
    }

    [Fact]
    public async Task CachedCompletedInt32Task_Reuses_Task_For_Same_Result()
    {
        // Assemble
        var cache = new CachedCompletedInt32Task();

        // Act
        var task1 = cache.GetTask(42);
        var task2 = cache.GetTask(42);

        // Assert
        Assert.Same(task1, task2);
        Assert.Equal(42, await task1);
    }

    [Fact]
    public async Task CachedCompletedInt32Task_Creates_New_Task_For_Different_Result()
    {
        // Assemble
        var cache = new CachedCompletedInt32Task();

        // Act
        var task1 = cache.GetTask(42);
        var task2 = cache.GetTask(43);

        // Assert
        Assert.NotSame(task1, task2);
        Assert.Equal(42, await task1);
        Assert.Equal(43, await task2);
    }

    [Fact]
    public void ToArray_Excludes_Trailing_Zeros_From_ArrayPool_Buffer()
    {
        // Assemble
        var stream = GetStream(); // Initial capacity may be > 0 due to ArrayPool min 16
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        stream.Write(data, 0, 5);
        var result = stream.ToArray();

        // Assert
        Assert.Equal(5, result.Length);
        Assert.Equal(data, result);
        // Ensure no unexpected trailing zeros (ArrayPool may allocate more than needed)
        for (int i = 0; i < result.Length; i++)
        {
            Assert.NotEqual(0, result[i]); // All written bytes are non-zero
        }
    }

    [Fact]
    public void NonExpandable_Stream_Write_Beyond_Capacity_Throws_NotSupportedException()
    {
        // Assemble
        byte[] buffer = new byte[10];
        var stream = new PooledMemoryStream(buffer, 0, 10, true, true); // writable, but _expandable = false for buffer constructors

        // Act & Assert
        // Try to write more than capacity
        byte[] data = new byte[20];
        Assert.Throws<NotSupportedException>(() => stream.Write(data, 0, 20));
    }

    [Fact]
    public void NonExpandable_Stream_SetLength_Beyond_Capacity_Throws_NotSupportedException()
    {
        // Assemble
        byte[] buffer = new byte[10];
        var stream = new PooledMemoryStream(buffer, 0, 10, true, true);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => stream.SetLength(20));
    }

    [Fact]
    public void NonExpandable_Stream_Capacity_Set_Different_Value_Throws_NotSupportedException()
    {
        // Assemble
        byte[] buffer = new byte[10];
        var stream = new PooledMemoryStream(buffer, 0, 10, true, true);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => stream.Capacity = 20);
    }

    [Fact]
    public async Task ReadAsync_ByteArray_Cancellation_Throws_TaskCanceledException()
    {
        // Assemble
        var stream = GetStream();
        stream.Write([1, 2, 3], 0, 3);
        stream.Position = 0;
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        byte[] buffer = new byte[3];

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => stream.ReadAsync(buffer, 0, 3, cts.Token));
    }

    [Fact]
    public async Task WriteAsync_ByteArray_Cancellation_Throws_TaskCanceledException()
    {
        // Assemble
        var stream = GetStream();
        byte[] buffer = [1, 2, 3];
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => stream.WriteAsync(buffer, 0, 3, cts.Token));
    }

    [Fact]
    public async Task FlushAsync_Cancellation_Throws_TaskCanceledException()
    {
        // Assemble
        var stream = GetStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => stream.FlushAsync(cts.Token));
    }

    [Fact]
    public void Write_Invalid_Buffer_Throws_ArgumentNullException()
    {
        // Assemble
        var stream = GetStream();

        // Act & Assert
#pragma warning disable CS8625
        Assert.Throws<ArgumentNullException>(() => stream.Write(null, 0, 1));
#pragma warning restore CS8625
    }

    [Fact]
    public void Write_Invalid_Offset_Throws_ArgumentOutOfRangeException()
    {
        // Assemble
        var stream = GetStream();
        byte[] buffer = [1, 2, 3];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(buffer, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(buffer, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(buffer, 4, 1)); // offset + count > buffer.Length
    }

    [Fact]
    public void Read_Invalid_Buffer_Throws_ArgumentNullException()
    {
        // Assemble
        var stream = GetStream();

        // Act & Assert
#pragma warning disable CS8625
        Assert.Throws<ArgumentNullException>(() => stream.Read(null, 0, 1));
#pragma warning restore CS8625
    }

    [Fact]
    public void Read_Invalid_Offset_Throws_ArgumentOutOfRangeException()
    {
        // Assemble
        var stream = GetStream();
        byte[] buffer = new byte[3];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer, 4, 1)); // offset + count > buffer.Length
    }

    [Fact]
    public void Seek_Invalid_Origin_Throws_ArgumentException()
    {
        // Assemble
        var stream = GetStream();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stream.Seek(0, (SeekOrigin)999));
    }

    [Fact]
    public void GetBuffer_On_NonExposable_Stream_Throws_UnauthorizedAccessException()
    {
        // Assemble
        byte[] buffer = [1, 2, 3];
        var stream = new PooledMemoryStream(buffer, 0, 3, true, false); // publiclyVisible = false

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => stream.GetBuffer());
    }

    [Fact]
    public void TryGetBuffer_On_NonExposable_Stream_Returns_False()
    {
        // Assemble
        byte[] buffer = [1, 2, 3];
        var stream = new PooledMemoryStream(buffer, 0, 3, true, false);

        // Act
        var success = stream.TryGetBuffer(out var segment);

        // Assert
        Assert.False(success);
        Assert.Equal(0, segment.Count);
    }

    [Fact]
    public void WriteByte_On_ReadOnly_Stream_Throws_NotSupportedException()
    {
        // Assemble
        byte[] buffer = [1, 2, 3];
        var stream = new PooledMemoryStream(buffer, 0, 3, false, true); // writable = false

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(4));
    }

    [Fact]
    public void Write_On_ReadOnly_Stream_Throws_NotSupportedException()
    {
        // Assemble
        byte[] buffer = [1, 2, 3];
        var stream = new PooledMemoryStream(buffer, 0, 3, false, true);

        // Act & Assert
        byte[] data = [4, 5];
        Assert.Throws<NotSupportedException>(() => stream.Write(data, 0, 2));
    }

    [Fact]
    public void CopyTo_Null_Destination_Throws_ArgumentNullException()
    {
        // Assemble
        var stream = GetStream();

        // Act & Assert
#pragma warning disable CS8625
        Assert.Throws<ArgumentNullException>(() => stream.CopyTo(null));
#pragma warning restore CS8625
    }

    [Fact]
    public void WriteTo_Null_Stream_Throws_ArgumentNullException()
    {
        // Assemble
        var stream = GetStream();

        // Act & Assert
#pragma warning disable CS8625
        Assert.Throws<ArgumentNullException>(() => stream.WriteTo(null));
#pragma warning restore CS8625
    }

    [Fact]
    public async Task CopyToAsync_With_File_Stream_Using_MockFileSystem_Works()
    {
        // Assemble
        var mockFileSystem = new MockFileSystem();
        var stream = GetStream();
        stream.Write([1, 2, 3, 4, 5], 0, 5);
        stream.Position = 0;

        // Act
        using (var fileStream = mockFileSystem.File.OpenWrite("test.txt"))
        {
            await stream.CopyToAsync(fileStream, 1024, TestContext.Current.CancellationToken);
        }

        // Assert
        var fileContent = mockFileSystem.File.ReadAllBytes("test.txt");
        Assert.Equal([1, 2, 3, 4, 5], fileContent);
    }

    [Fact]
    public void CopyTo_With_TestPooledMemoryStream_Subclass_Uses_Base_Implementation()
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3, 4, 5], 0, 5);
        source.Position = 0;
        var destination = new PooledMemoryStream();

        // Act
        source.CopyTo(destination, 1024);

        // Assert
        Assert.Equal(5, destination.Length);
        Assert.Equal([1, 2, 3, 4, 5], destination.ToArray());
    }

    [Fact]
    public async Task CopyToAsync_With_TestPooledMemoryStream_Subclass_Uses_Base_Implementation()
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3, 4, 5], 0, 5);
        source.Position = 0;
        var destination = new PooledMemoryStream();

        // Act
        await source.CopyToAsync(destination, 1024, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, destination.Length);
        Assert.Equal([1, 2, 3, 4, 5], destination.ToArray());
    }

    [Fact]
    public void CopyTo_With_TestPooledMemoryStream_Subclass_And_BufferSize_Larger_Than_Data_Works()
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 0;
        var destination = new PooledMemoryStream();

        // Act
        source.CopyTo(destination, 10); // bufferSize larger than data

        // Assert
        Assert.Equal(3, destination.Length);
        Assert.Equal([1, 2, 3], destination.ToArray());
    }

    [Fact]
    public async Task CopyToAsync_With_TestPooledMemoryStream_Subclass_And_BufferSize_Larger_Than_Data_Works()
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 0;
        var destination = new PooledMemoryStream();

        // Act
        await source.CopyToAsync(destination, 10, TestContext.Current.CancellationToken); // bufferSize larger than data

        // Assert
        Assert.Equal(3, destination.Length);
        Assert.Equal([1, 2, 3], destination.ToArray());
    }

    [Fact]
    public void CopyTo_With_TestPooledMemoryStream_Subclass_At_End_Of_Stream_Does_Nothing()
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 3; // At end
        var destination = new PooledMemoryStream();

        // Act
        source.CopyTo(destination, 1024);

        // Assert
        Assert.Equal(0, destination.Length);
    }

    [Fact]
    public async Task CopyToAsync_With_TestPooledMemoryStream_Subclass_At_End_Of_Stream_Does_Nothing()
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 3; // At end
        var destination = new PooledMemoryStream();

        // Act
        await source.CopyToAsync(destination, 1024, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, destination.Length);
    }

    [Fact]
    public void CopyTo_With_TestPooledMemoryStream_Subclass_Null_Destination_Throws_ArgumentNullException()
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 0;

        // Act & Assert
        var act = () => source.CopyTo(null!, 1024);
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public async Task CopyToAsync_With_TestPooledMemoryStream_Subclass_Null_Destination_Throws_ArgumentNullException()
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 0;

        // Act & Assert
        var act = () => source.CopyToAsync(null!, 1024, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CopyTo_With_TestPooledMemoryStream_Subclass_Invalid_BufferSize_Throws_ArgumentOutOfRangeException(int bufferSize)
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 0;
        var destination = new PooledMemoryStream();

        // Act & Assert
        var act = () => source.CopyTo(destination, bufferSize);
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CopyToAsync_With_TestPooledMemoryStream_Subclass_Invalid_BufferSize_Throws_ArgumentOutOfRangeException(int bufferSize)
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 0;
        var destination = new PooledMemoryStream();

        // Act & Assert
        var act = () => source.CopyToAsync(destination, bufferSize, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task CopyToAsync_With_TestPooledMemoryStream_Subclass_Cancellation_Throws_TaskCanceledException()
    {
        // Assemble
        var source = new TestPooledMemoryStream();
        source.Write([1, 2, 3], 0, 3);
        source.Position = 0;
        var destination = new PooledMemoryStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = () => source.CopyToAsync(destination, 1024, cts.Token);
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

    [Fact]
    public async Task WriteAsync_ByteArray_OperationCanceledException_From_Write_Throws_TaskCanceledException()
    {
        // Assemble
        var stream = new ThrowingWritePooledMemoryStream();
        byte[] buffer = [1, 2, 3];

        // Act
        var act = () => stream.WriteAsync(buffer, 0, 3, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

    [Fact]
    public async Task ReadAsync_ByteArray_OperationCanceledException_From_Read_Throws_TaskCanceledException()
    {
        // Assemble
        var stream = new ThrowingReadPooledMemoryStream();
        stream.Write([1, 2, 3], 0, 3);
        stream.Position = 0;
        byte[] buffer = new byte[3];

        // Act
        var act = () => stream.ReadAsync(buffer, 0, 3, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

    [Fact]
    public async Task WriteAsync_Memory_OperationCanceledException_From_Write_Throws_TaskCanceledException()
    {
        // Assemble
        var stream = new ThrowingWriteSpanPooledMemoryStream();
        byte[] arr = [1, 2, 3];
        ReadOnlyMemory<byte> buffer = arr;

        // Act
        var act = async () => await stream.WriteAsync(buffer, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
    }

}
