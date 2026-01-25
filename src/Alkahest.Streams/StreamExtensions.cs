using System.Buffers;

namespace Alkahest.Streams;

/// <summary>
/// Provides extension methods for Stream operations.
/// </summary>
public static class StreamExtensions
{
    extension(Stream s)
    {
        /// <summary>
        /// Asynchronously copies the contents of the stream to the destination stream using a pooled buffer.
        /// </summary>
        /// <param name="destination">The stream to copy to.</param>
        /// <param name="bufferSize">The size of the buffer to use. Default is 81920 bytes.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CopyWithPooledBufferAsync(Stream destination, int bufferSize = 81920)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = await s.ReadAsync(buffer)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}

