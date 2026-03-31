using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Storage
{

    /// <summary>
    /// A stream wrapper that limits reading to a specified number of bytes.
    /// Used to work around HttpListener's ChunkedInputStream issues on macOS.
    /// </summary>
    internal sealed class LengthLimitedStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _maxLength;
        private long _bytesRead;

        /// <inheritdoc/>
        public override bool CanRead => _innerStream.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => _maxLength;

        /// <inheritdoc/>
        public override long Position
        {
            get => _bytesRead;
            set => throw new NotSupportedException();
        }

        public LengthLimitedStream(Stream innerStream, long maxLength)
        {
            _innerStream = innerStream;
            _maxLength = maxLength;
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _maxLength - _bytesRead;
            if (remaining <= 0)
                return 0;

            var toRead = (int)Math.Min(count, remaining);
            var bytesRead = _innerStream.Read(buffer, offset, toRead);
            _bytesRead += bytesRead;
            return bytesRead;
        }

        /// <inheritdoc/>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            var remaining = _maxLength - _bytesRead;
            if (remaining <= 0)
                return 0;

            var toRead = (int)Math.Min(count, remaining);
            var bytesRead = await _innerStream.ReadAsync(buffer, offset, toRead, cancellationToken)
                .ConfigureAwait(false);
            _bytesRead += bytesRead;
            return bytesRead;
        }

        /// <inheritdoc/>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var remaining = _maxLength - _bytesRead;
            if (remaining <= 0)
                return 0;

            var toRead = (int)Math.Min(buffer.Length, remaining);
            var bytesRead = await _innerStream.ReadAsync(buffer[..toRead], cancellationToken).ConfigureAwait(false);
            _bytesRead += bytesRead;
            return bytesRead;
        }

        /// <inheritdoc/>
        public override void Flush() => _innerStream.Flush();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}