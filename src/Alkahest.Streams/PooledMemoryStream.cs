using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Alkahest.Streams;

[ExcludeFromCodeCoverage]
internal struct CachedCompletedInt32Task
{
    private Task<int>? _task;

    /// <summary>Gets a completed <see cref="Task{Int32}"/> whose result is <paramref name="result"/>.</summary>
    /// <remarks>This method will try to return an already cached task if available.</remarks>
    /// <param name="result">The result value for which a <see cref="Task{Int32}"/> is needed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> GetTask(int result)
    {
        if (_task is Task<int> task)
        {
            Debug.Assert(task.IsCompletedSuccessfully, "Expected that a stored last task completed successfully");
            if (task.Result == result)
            {
                return task;
            }
        }

        return _task = Task.FromResult(result);
    }
}

/// <summary>
/// Represents a stream backed by a pooled byte array rented from <see cref="ArrayPool{T}"/> for efficient memory management.
/// This class provides functionality equivalent to <see cref="MemoryStream"/> but uses rented buffers to minimize allocations,
/// reduce garbage collection pressure, and improve performance in high-throughput scenarios.
/// </summary>
/// <remarks>
/// The internal buffer is rented from <see cref="ArrayPool{T}.Shared"/> and returned when the stream is disposed.
/// For security, the buffer is zeroed before returning to the pool.
/// When the capacity is expanded, a new buffer is rented, data is copied, and the old buffer is returned.
/// </remarks>
public class PooledMemoryStream : Stream
{
    private byte[] _pooledBuffer; // Allocated internally.
    private readonly int _origin;       // For user-provided arrays, start at this origin
    private int _position;     // read/write head.
    private int _length;       // Number of bytes within the memory stream
    private int _capacity;     // length of usable portion of buffer for stream
                               // Note that _capacity == _pooledBuffer.Length for non-user-provided byte[]'s

    private bool _expandable;  // User-provided buffers aren't expandable.
    private bool _writable;    // Can user write to this stream?
    private readonly bool _exposable;   // Whether the array can be returned to the user.
    private bool _isOpen;      // Is this stream open or closed?

    private CachedCompletedInt32Task _lastReadTask; // The last successful task returned from ReadAsync

    private const int MemStreamMaxLength = int.MaxValue - 56; // Array.MaxLength

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledMemoryStream"/> class with an initial capacity of 0.
    /// </summary>
    public PooledMemoryStream() : this(0) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledMemoryStream"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial size of the internal buffer in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public PooledMemoryStream(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        _pooledBuffer = ArrayPool<byte>.Shared.Rent(capacity);
        _capacity = capacity;
        _expandable = true;
        _writable = true;
        _exposable = true;
        _isOpen = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledMemoryStream"/> class based on the specified byte array.
    /// </summary>
    /// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    public PooledMemoryStream(byte[] buffer)
        : this(buffer, true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledMemoryStream"/> class based on the specified byte array with the specified access permission.
    /// </summary>
    /// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
    /// <param name="writable">The setting of the <see cref="CanWrite"/> property, which determines whether the stream supports writing.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    public PooledMemoryStream(byte[] buffer, bool writable)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        _pooledBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);

        if (buffer.Length > 0)
        {
            Buffer.BlockCopy(buffer, 0, _pooledBuffer, 0, buffer.Length);
        }

        _length = buffer.Length;
        _capacity = _pooledBuffer.Length;
        _writable = writable;
        _isOpen = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledMemoryStream"/> class based on the specified region of the specified byte array.
    /// </summary>
    /// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
    /// <param name="index">The index into <paramref name="buffer"/> at which the stream begins.</param>
    /// <param name="count">The length of the stream in bytes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentException">The buffer length minus <paramref name="index"/> is less than <paramref name="count"/>.</exception>
    public PooledMemoryStream(byte[] buffer, int index, int count)
        : this(buffer, index, count, true, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledMemoryStream"/> class based on the specified region of the specified byte array with the specified access permission.
    /// </summary>
    /// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
    /// <param name="index">The index into <paramref name="buffer"/> at which the stream begins.</param>
    /// <param name="count">The length of the stream in bytes.</param>
    /// <param name="writable">The setting of the <see cref="CanWrite"/> property, which determines whether the stream supports writing.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentException">The buffer length minus <paramref name="index"/> is less than <paramref name="count"/>.</exception>
    public PooledMemoryStream(byte[] buffer, int index, int count, bool writable)
        : this(buffer, index, count, writable, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledMemoryStream"/> class based on the specified region of the specified byte array with the specified access permission and visibility.
    /// </summary>
    /// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
    /// <param name="index">The index into <paramref name="buffer"/> at which the stream begins.</param>
    /// <param name="count">The length of the stream in bytes.</param>
    /// <param name="writable">The setting of the <see cref="CanWrite"/> property, which determines whether the stream supports writing.</param>
    /// <param name="publiclyVisible">The setting of the <see cref="GetBuffer"/> and <see cref="TryGetBuffer"/> methods, which determines whether the stream's buffer can be accessed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentException">The buffer length minus <paramref name="index"/> is less than <paramref name="count"/>.</exception>
    public PooledMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (buffer.Length - index < count)
            throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

        _pooledBuffer = ArrayPool<byte>.Shared.Rent(count);

        if (buffer.Length > 0)
        {
            Buffer.BlockCopy(buffer, index, _pooledBuffer, 0, index + count);
        }

        _origin = _position = index;
        _length = _capacity = index + count;
        _writable = writable;
        _exposable = publiclyVisible;  // Can TryGetBuffer/GetBuffer return the array?
        _isOpen = true;
    }

    /// <inheritdoc/>
    public override bool CanRead => _isOpen;

    /// <inheritdoc/>
    public override bool CanSeek => _isOpen;

    /// <inheritdoc/>
    public override bool CanWrite => _writable;

    private void EnsureNotClosed()
    {
        if (!_isOpen)
            throw new ObjectDisposedException(null, "Cannot access a closed stream.");
    }

    private void EnsureWriteable()
    {
        if (!CanWrite)
            throw new NotSupportedException("Stream does not support writing.");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The internal buffer is zeroed for security before being returned to the pool.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isOpen = false;
            _writable = false;
            _expandable = false;

            // Don't set buffer to null - allow TryGetBuffer, GetBuffer & ToArray to work.
            CryptographicOperations.ZeroMemory(_pooledBuffer.AsSpan(0, _length));
            ArrayPool<byte>.Shared.Return(_pooledBuffer, clearArray: true);

            _lastReadTask = default;
        }
    }

    // returns a bool saying whether we allocated a new array.
    private bool EnsureCapacity(int value)
    {
        // Check for overflow
        if (value < 0)
            throw new IOException("Stream was too long.");

        if (value > _capacity)
        {
            int newCapacity = Math.Max(value, 256);

            // We are ok with this overflowing since the next statement will deal
            // with the cases where _capacity*2 overflows.
            if (newCapacity < _capacity * 2)
            {
                newCapacity = _capacity * 2;
            }

            // We want to expand the array up to Array.MaxLength.
            // And we want to give the user the value that they asked for
            if ((uint)(_capacity * 2) > Array.MaxLength)
            {
                newCapacity = Math.Max(value, Array.MaxLength);
            }

            Capacity = newCapacity;
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public override void Flush()
    {
    }

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try
        {
            Flush();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the internal pooled buffer. The buffer remains valid until the stream is disposed.
    /// </remarks>
    public virtual byte[] GetBuffer()
    {
        if (!_exposable)
            throw new UnauthorizedAccessException("PooledMemoryStream's internal buffer cannot be accessed.");
        return _pooledBuffer;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns a segment of the internal pooled buffer. The buffer remains valid until the stream is disposed.
    /// </remarks>
    public virtual bool TryGetBuffer(out ArraySegment<byte> buffer)
    {
        if (!_exposable)
        {
            buffer = default;
            return false;
        }

        buffer = new ArraySegment<byte>(_pooledBuffer, offset: _origin, count: _length - _origin);
        return true;
    }

    // -------------- PERF: Internal functions for fast direct access of PooledMemoryStream buffer (cf. BinaryReader for usage) ---------------

    // PERF: Internal sibling of GetBuffer, always returns a buffer (cf. GetBuffer())
    internal byte[] InternalGetBuffer()
    {
        return _pooledBuffer;
    }

    // PERF: True cursor position, we don't need _origin for direct access
    internal int InternalGetPosition()
    {
        return _position;
    }

    // PERF: Expose internal buffer for BinaryReader instead of going via the regular Stream interface which requires to copy the data out
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<byte> InternalReadSpan(int count)
    {
        EnsureNotClosed();

        int origPos = _position;
        int newPos = origPos + count;

        if ((uint)newPos > (uint)_length)
        {
            _position = _length;
            throw new EndOfStreamException("Unable to read beyond the end of the stream.");
        }

        var span = new ReadOnlySpan<byte>(_pooledBuffer, origPos, count);
        _position = newPos;
        return span;
    }

    // PERF: Get actual length of bytes available for read; do sanity checks; shift position - i.e. everything except actual copying bytes
    internal int InternalEmulateRead(int count)
    {
        EnsureNotClosed();

        int n = _length - _position;
        if (n > count)
            n = count;
        if (n < 0)
            n = 0;

        Debug.Assert(_position + n >= 0);  // len is less than 2^31 -1.
        _position += n;
        return n;
    }

    /// <summary>
    /// Gets or sets the capacity of the stream.
    /// </summary>
    /// <value>The capacity of the stream in bytes.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or less than the current length.</exception>
    /// <exception cref="NotSupportedException">The stream is not expandable.</exception>
    /// <remarks>
    /// When setting a larger capacity, a new buffer is rented from the pool, data is copied, and the old buffer is returned.
    /// </remarks>
    public virtual int Capacity
    {
        get
        {
            EnsureNotClosed();
            return _capacity - _origin;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(Capacity));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Array.MaxLength, nameof(Capacity));

            // Only update the capacity if the MS is expandable and the value is different than the current capacity.
            // Special behavior if the MS isn't expandable: we don't throw if value is the same as the current capacity
            if (value < Length)
                throw new ArgumentOutOfRangeException(nameof(value), "capacity was less than the current size.");

            EnsureNotClosed();

            if (!_expandable && (value != Capacity))
                throw new NotSupportedException("PooledMemory stream is not expandable.");

            // PooledMemoryStream has this invariant: _origin > 0 => !expandable (see ctors)
            if (_expandable && value != _capacity)
            {
                if (value > 0)
                {
                    byte[] newBuffer = ArrayPool<byte>.Shared.Rent(value);

                    if (_length > 0)
                    {
                        Buffer.BlockCopy(_pooledBuffer, 0, newBuffer, 0, _length);
                    }
                    CryptographicOperations.ZeroMemory(_pooledBuffer);
                    ArrayPool<byte>.Shared.Return(_pooledBuffer, clearArray: true); // No need to clear full if we zeroed written

                    _pooledBuffer = newBuffer;
                }
                else
                {
                    byte[] newBuffer = ArrayPool<byte>.Shared.Rent(0);

                    CryptographicOperations.ZeroMemory(_pooledBuffer);
                    ArrayPool<byte>.Shared.Return(_pooledBuffer, clearArray: true); // No need to clear full if we zeroed written

                    _pooledBuffer = newBuffer;
                }

                _capacity = value;
            }
        }
    }

    /// <inheritdoc/>
    public override long Length
    {
        get
        {
            EnsureNotClosed();
            return _length - _origin;
        }
    }

    /// <inheritdoc/>
    public override long Position
    {
        get
        {
            EnsureNotClosed();
            return _position - _origin;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            EnsureNotClosed();

            if (value > MemStreamMaxLength - _origin)
                throw new ArgumentOutOfRangeException(nameof(value), "Stream length must be non-negative and less than 2^31 - 1 - origin.");
            _position = _origin + (int)value;
        }
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        EnsureNotClosed();

        int n = _length - _position;
        if (n > count)
            n = count;
        if (n <= 0)
            return 0;

        Debug.Assert(_position + n >= 0);  // len is less than 2^31 -1.

        if (n <= 8)
        {
            int byteCount = n;
            while (--byteCount >= 0)
                buffer[offset + byteCount] = _pooledBuffer[_position + byteCount];
        }
        else
            Buffer.BlockCopy(_pooledBuffer, _position, buffer, offset, n);

        _position += n;

        return n;
    }

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        if (GetType() != typeof(PooledMemoryStream))
        {
            // PooledMemoryStream is not sealed, and a derived type may have overridden Read(byte[], int, int) prior
            // to this Read(Span<byte>) overload being introduced.  In that case, this Read(Span<byte>) overload
            // should use the behavior of Read(byte[],int,int) overload.
            return base.Read(buffer);
        }

        EnsureNotClosed();

        int n = Math.Min(_length - _position, buffer.Length);
        if (n <= 0)
            return 0;

        new Span<byte>(_pooledBuffer, _position, n).CopyTo(buffer);

        _position += n;
        return n;
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        // If cancellation was requested, bail early
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<int>(cancellationToken);

        try
        {
            int n = Read(buffer, offset, count);
            return _lastReadTask.GetTask(n);
        }
        catch (OperationCanceledException oce)
        {
            return Task.FromCanceled<int>(oce.CancellationToken);
        }
        catch (Exception exception)
        {
            return Task.FromException<int>(exception);
        }
    }

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }

        try
        {
            // ReadAsync(Memory<byte>,...) needs to delegate to an existing virtual to do the work, in case an existing derived type
            // has changed or augmented the logic associated with reads.  If the Memory wraps an array, we could delegate to
            // ReadAsync(byte[], ...), but that would defeat part of the purpose, as ReadAsync(byte[], ...) often needs to allocate
            // a Task<int> for the return value, so we want to delegate to one of the synchronous methods.  We could always
            // delegate to the Read(Span<byte>) method, and that's the most efficient solution when dealing with a concrete
            // PooledMemoryStream, but if we're dealing with a type derived from PooledMemoryStream, Read(Span<byte>) will end up delegating
            // to Read(byte[], ...), which requires it to get a byte[] from ArrayPool and copy the data.  So, we special-case the
            // very common case of the Memory<byte> wrapping an array: if it does, we delegate to Read(byte[], ...) with it,
            // as that will be efficient in both cases, and we fall back to Read(Span<byte>) if the Memory<byte> wrapped something
            // else; if this is a concrete PooledMemoryStream, that'll be efficient, and only in the case where the Memory<byte> wrapped
            // something other than an array and this is a PooledMemoryStream-derived type that doesn't override Read(Span<byte>) will
            // it then fall back to doing the ArrayPool/copy behavior.
            return new ValueTask<int>(
                MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> destinationArray) ?
                    Read(destinationArray.Array!, destinationArray.Offset, destinationArray.Count) :
                    Read(buffer.Span));
        }
        catch (OperationCanceledException oce)
        {
            return new ValueTask<int>(Task.FromCanceled<int>(oce.CancellationToken));
        }
        catch (Exception exception)
        {
            return ValueTask.FromException<int>(exception);
        }
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        EnsureNotClosed();

        if (_position >= _length)
            return -1;

        return _pooledBuffer[_position++];
    }

    /// <inheritdoc/>
    public override void CopyTo(Stream destination, int bufferSize)
    {
        // If we have been inherited into a subclass, the following implementation could be incorrect
        // since it does not call through to Read() which a subclass might have overridden.
        // To be safe we will only use this implementation in cases where we know it is safe to do so,
        // and delegate to our base class (which will call into Read) when we are not sure.
        if (GetType() != typeof(PooledMemoryStream))
        {
            base.CopyTo(destination, bufferSize);
            return;
        }

        // Validate the arguments the same way Stream does for back-compat.
        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotClosed();

        int originalPosition = _position;

        // Seek to the end of the PooledMemoryStream.
        int remaining = InternalEmulateRead(_length - originalPosition);

        // If we were already at or past the end, there's no copying to do so just quit.
        if (remaining > 0)
        {
            // Call Write() on the other Stream, using our internal buffer and avoiding any
            // intermediary allocations.
            destination.Write(_pooledBuffer, originalPosition, remaining);
        }
    }

    /// <inheritdoc/>
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        // This implementation offers better performance compared to the base class version.

        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotClosed();

        // If we have been inherited into a subclass, the following implementation could be incorrect
        // since it does not call through to ReadAsync() which a subclass might have overridden.
        // To be safe we will only use this implementation in cases where we know it is safe to do so,
        // and delegate to our base class (which will call into ReadAsync) when we are not sure.
        if (GetType() != typeof(PooledMemoryStream))
            return base.CopyToAsync(destination, bufferSize, cancellationToken);

        // If canceled - return fast:
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        // Avoid copying data from this buffer into a temp buffer:
        // (require that InternalEmulateRead does not throw,
        // otherwise it needs to be wrapped into try-catch-Task.FromException like memStrDest.Write below)

        int pos = _position;
        int n = InternalEmulateRead(_length - _position);

        // If we were already at or past the end, there's no copying to do so just quit.
        if (n == 0)
            return Task.CompletedTask;

        // If destination is not a memory stream, write there asynchronously:
        if (destination is not PooledMemoryStream memStrDest)
            return destination.WriteAsync(_pooledBuffer, pos, n, cancellationToken);

        try
        {
            // If destination is a PooledMemoryStream, CopyTo synchronously:
            memStrDest.Write(_pooledBuffer, pos, n);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin loc)
    {
        EnsureNotClosed();

        return SeekCore(offset, loc switch
        {
            SeekOrigin.Begin => _origin,
            SeekOrigin.Current => _position,
            SeekOrigin.End => _length,
            _ => throw new ArgumentException("Invalid seek origin.")
        });
    }

    private long SeekCore(long offset, int loc)
    {
        if (offset > MemStreamMaxLength - loc)
            throw new ArgumentOutOfRangeException(nameof(offset), "Stream length must be non-negative and less than or equal to Int32.MaxValue.");
        int tempPosition = unchecked(loc + (int)offset);
        if (unchecked(loc + offset) < _origin || tempPosition < _origin)
            throw new IOException("An attempt was made to move the position before the beginning of the stream.");
        _position = tempPosition;

        Debug.Assert(_position >= _origin);
        return _position - _origin;
    }

    // Sets the length of the stream to a given value.  The new
    // value must be nonnegative and less than the space remaining in
    // the array, int.MaxValue - origin
    // Origin is 0 in all cases other than a PooledMemoryStream created on
    // top of an existing array and a specific starting offset was passed
    // into the PooledMemoryStream constructor.  The upper bounds prevents any
    // situations where a stream may be created on top of an array then
    // the stream is made longer than the maximum possible length of the
    // array (int.MaxValue).
    //
    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        if (value < 0 || value > Array.MaxLength)
            throw new ArgumentOutOfRangeException(nameof(value), "Stream length must be non-negative and less than or equal to Array.MaxLength.");

        EnsureWriteable();

        // Origin wasn't publicly exposed above.
        Debug.Assert(MemStreamMaxLength == Array.MaxLength);  // Check parameter validation logic in this method if this fails.
        if (value > (Array.MaxLength - _origin))
            throw new ArgumentOutOfRangeException(nameof(value), "Stream length must be non-negative and less than or equal to Array.MaxLength.");

        int newLength = _origin + (int)value;
        bool allocatedNewArray = EnsureCapacity(newLength);
        if (!allocatedNewArray && newLength > _length)
            Array.Clear(_pooledBuffer, _length, newLength - _length);
        _length = newLength;
        if (_position > newLength)
            _position = newLength;
    }

    /// <inheritdoc/>
    public virtual byte[] ToArray()
    {
        int count = _length - _origin;
        if (count == 0)
            return [];
        byte[] copy = GC.AllocateUninitializedArray<byte>(count);
        _pooledBuffer.AsSpan(_origin, count).CopyTo(copy);
        return copy;
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        EnsureNotClosed();
        EnsureWriteable();

        int i = _position + count;
        // Check for overflow
        if (i < 0)
            throw new IOException("Stream was too long.");

        if (i > _length)
        {
            bool mustZero = _position > _length;
            if (i > _capacity)
            {
                bool allocatedNewArray = EnsureCapacity(i);
                if (allocatedNewArray)
                {
                    mustZero = false;
                }
            }
            if (mustZero)
            {
                Array.Clear(_pooledBuffer, _length, i - _length);
            }
            _length = i;
        }
        if ((count <= 8) && (buffer != _pooledBuffer))
        {
            int byteCount = count;
            while (--byteCount >= 0)
            {
                _pooledBuffer[_position + byteCount] = buffer[offset + byteCount];
            }
        }
        else
        {
            Buffer.BlockCopy(buffer, offset, _pooledBuffer, _position, count);
        }
        _position = i;
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (GetType() != typeof(PooledMemoryStream))
        {
            // PooledMemoryStream is not sealed, and a derived type may have overridden Write(byte[], int, int) prior
            // to this Write(Span<byte>) overload being introduced.  In that case, this Write(Span<byte>) overload
            // should use the behavior of Write(byte[],int,int) overload.
            base.Write(buffer);
            return;
        }

        EnsureNotClosed();
        EnsureWriteable();

        // Check for overflow
        int i = _position + buffer.Length;
        if (i < 0)
            throw new IOException("Stream was too long.");

        if (i > _length)
        {
            bool mustZero = _position > _length;
            if (i > _capacity)
            {
                bool allocatedNewArray = EnsureCapacity(i);
                if (allocatedNewArray)
                {
                    mustZero = false;
                }
            }
            if (mustZero)
            {
                Array.Clear(_pooledBuffer, _length, i - _length);
            }
            _length = i;
        }

        buffer.CopyTo(new Span<byte>(_pooledBuffer, _position, buffer.Length));
        _position = i;
    }

    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        // If cancellation is already requested, bail early
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }
        catch (OperationCanceledException oce)
        {
            return Task.FromCanceled(oce.CancellationToken);
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        try
        {
            // See corresponding comment in ReadAsync for why we don't just always use Write(ReadOnlySpan<byte>).
            // Unlike ReadAsync, we could delegate to WriteAsync(byte[], ...) here, but we don't for consistency.
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> sourceArray))
            {
                Write(sourceArray.Array!, sourceArray.Offset, sourceArray.Count);
            }
            else
            {
                Write(buffer.Span);
            }
            return default;
        }
        catch (OperationCanceledException oce)
        {
            return new ValueTask(Task.FromCanceled(oce.CancellationToken));
        }
        catch (Exception exception)
        {
            return ValueTask.FromException(exception);
        }
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        EnsureNotClosed();
        EnsureWriteable();

        if (_position >= _length)
        {
            int newLength = _position + 1;
            bool mustZero = _position > _length;
            if (newLength >= _capacity)
            {
                bool allocatedNewArray = EnsureCapacity(newLength);
                if (allocatedNewArray)
                {
                    mustZero = false;
                }
            }
            if (mustZero)
            {
                Array.Clear(_pooledBuffer, _length, _position - _length);
            }
            _length = newLength;
        }
        _pooledBuffer[_position++] = value;
    }

    // Writes this PooledMemoryStream to another stream.

    /// <inheritdoc/>
    public virtual void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        EnsureNotClosed();

        stream.Write(_pooledBuffer, _origin, _length - _origin);
    }

}
