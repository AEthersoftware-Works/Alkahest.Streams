using System.Text;
using Testably.Abstractions.Testing;

namespace Alkahest.Streams.Tests;

public class TrackingStreamWriterTests
{
    [Fact]
    public async Task Counts_Bytes_On_Write()
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
    public async Task Counts_Copied_Bytes()
    {
        // Assemble
        var inner = new MemoryStream();
        var tracking = new TrackingStreamWriter(inner);

        using var source = new MemoryStream([4, 5, 6, 7]);

        // Act
        await source.CopyToAsync(tracking, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(4, tracking.BytesWritten);
    }

    [Fact]
    public async Task Handles_Partial_Writes()
    {
        // Assemble
        var inner = new MemoryStream();
        var tracking = new TrackingStreamWriter(inner);

        // Act
        tracking.Write([1, 2, 3], 1, 2); // write 2,3

        // Assert
        Assert.Equal(2, tracking.BytesWritten);
    }

    [Fact]
    public void Write_Synchronous_Counts()
    {
        // Assemble
        var inner = new MemoryStream();
        var tracking = new TrackingStreamWriter(inner);
        var data = new byte[] { 1, 2, 3 };

        // Act
        tracking.Write(data, 0, 3);

        // Assert
        Assert.Equal(3, tracking.BytesWritten);
    }

    [Fact]
    public void WriteByte_Counts()
    {
        // Assemble
        var inner = new MemoryStream();
        var tracking = new TrackingStreamWriter(inner);

        // Act
        tracking.WriteByte(1);

        // Assert
        Assert.Equal(1, tracking.BytesWritten);
    }

    [Fact]
    public void Write_Span_Counts()
    {
        // Assemble
        var inner = new MemoryStream();
        var tracking = new TrackingStreamWriter(inner);
        var data = new byte[] { 1, 2, 3 };

        // Act
        tracking.Write(data.AsSpan());

        // Assert
        Assert.Equal(3, tracking.BytesWritten);
    }

    [Fact]
    public void Delegates_Properties()
    {
        // Assemble
        MockFileSystem fileSystem = new();
        fileSystem.File.Create("test.dat").Close();

        // Act
        using var inner = fileSystem.File.Open("test.dat", FileMode.Append, FileAccess.Write, FileShare.Read);
        var tracking = new TrackingStreamWriter(inner);

        // Assert
        Assert.True(tracking.CanWrite);
        Assert.False(tracking.CanRead);
        Assert.True(tracking.CanSeek);
    }

    [Fact]
    public async Task Flush_Delegates()
    {
        // Assemble
        string payloadContent = "🐬 So long and thanks for all the fish! 🐬";
        var payload = Encoding.UTF8.GetBytes(payloadContent);

        // Create a "virtual" file to check the contents after stream is flushed.
        MockFileSystem fileSystem = new();
        fileSystem.File.Create("test.dat").Close();
        string? preFlushContent = null,
                postFlushContent = null;

        // Act
        using var inner = fileSystem.File.Open("test.dat", FileMode.Append, FileAccess.Write, FileShare.Read);
        var tracking = new TrackingStreamWriter(inner);

        await tracking.WriteAsync(payload, TestContext.Current.CancellationToken);
        preFlushContent = await fileSystem.File.ReadAllTextAsync("test.dat", TestContext.Current.CancellationToken);

        await tracking.FlushAsync(TestContext.Current.CancellationToken);
        postFlushContent = await fileSystem.File.ReadAllTextAsync("test.dat", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(payloadContent, preFlushContent);
        Assert.Equal(payloadContent, postFlushContent);
    }

    [Fact]
    public void Length_Delegates()
    {
        // Assemble
        MockFileSystem fileSystem = new();
        fileSystem.File.WriteAllBytes("test.dat", [1, 2, 3, 4, 5]);
        using var inner = fileSystem.File.Open("test.dat", FileMode.Open, FileAccess.ReadWrite);
        var tracking = new TrackingStreamWriter(inner);

        // Act & Assert
        Assert.Equal(5, tracking.Length);
        Assert.Equal(0, tracking.BytesWritten); // unchanged
    }

    [Fact]
    public void Read_Delegates()
    {
        // Assemble
        MockFileSystem fileSystem = new();
        fileSystem.File.WriteAllBytes("test.dat", [1, 2, 3, 4, 5]);
        using var inner = fileSystem.File.Open("test.dat", FileMode.Open, FileAccess.ReadWrite);
        var tracking = new TrackingStreamWriter(inner);
        var buffer = new byte[3];

        // Act
        int read = tracking.Read(buffer, 0, 3);

        // Assert
        Assert.Equal(3, read);
        Assert.Equal([1, 2, 3], buffer);
        Assert.Equal(0, tracking.BytesWritten); // unchanged
    }

    [Fact]
    public void Seek_Delegates()
    {
        // Assemble
        MockFileSystem fileSystem = new();
        fileSystem.File.WriteAllBytes("test.dat", [1, 2, 3, 4, 5]);
        using var inner = fileSystem.File.Open("test.dat", FileMode.Open, FileAccess.ReadWrite);
        var tracking = new TrackingStreamWriter(inner);

        // Act
        long pos = tracking.Seek(2, SeekOrigin.Begin);

        // Assert
        Assert.Equal(2, pos);
        Assert.Equal(2, tracking.Position);
        Assert.Equal(0, tracking.BytesWritten); // unchanged
    }

    [Fact]
    public void SetLength_Delegates()
    {
        // Assemble
        MockFileSystem fileSystem = new();
        fileSystem.File.WriteAllBytes("test.dat", [1, 2, 3, 4, 5]);
        using var inner = fileSystem.File.Open("test.dat", FileMode.Open, FileAccess.ReadWrite);
        var tracking = new TrackingStreamWriter(inner);

        // Act
        tracking.SetLength(3);

        // Assert
        Assert.Equal(3, tracking.Length);
        Assert.Equal(0, tracking.BytesWritten); // unchanged
    }

}
