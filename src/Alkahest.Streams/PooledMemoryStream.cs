using System.Buffers;

namespace Alkahest.Streams;

/// <summary>
/// A MemoryStream that uses a pooled byte array from ArrayPool for efficient memory management.
/// </summary>
public sealed class PooledMemoryStream : MemoryStream
{
    private readonly byte[] _pooledArray;

    /// <summary>
    /// Initializes a new instance of PooledMemoryStream with the specified minimum capacity.
    /// </summary>
    /// <param name="minimumCapacity">The minimum capacity of the stream.</param>
    public PooledMemoryStream(int minimumCapacity)
        : base(ArrayPool<byte>.Shared.Rent(minimumCapacity), 0, minimumCapacity, writable: true, publiclyVisible: true)
    {
        _pooledArray = GetBuffer();
    }

    /// <summary>
    /// Disposes the stream and returns the pooled array to the pool.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var array = _pooledArray;
            if (array != null)
                ArrayPool<byte>.Shared.Return(array);
        }
        base.Dispose(disposing);
    }
}

