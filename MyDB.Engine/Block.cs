using System;
using System.Collections.Generic;
using System.IO;
using static MyDB.Engine.BlockHeader;
using static MyDB.Engine.ByteNumber;

namespace MyDB.Engine
{
    public class Block : IBlock
    {
        private readonly BlockOptions _options;

        private readonly Stream _stream;
        private readonly long _originPositionInStream;

        private readonly int _blockHeadersCount;
        private readonly IDictionary<int, long?> _cachedHeaders;

        private readonly byte[] _blockFirstSector;
        private bool _isBlockFirstSectorDirty;

        public long Id { get; }

        public bool IsDeleted => _cachedHeaders[BlockDeleted] > 0;

        public bool IsFirst => _cachedHeaders[PreviousBlock] == 0;

        public event EventHandler Disposed;

        public Block(long id, BlockOptions options, Stream stream)
        {
            Id = id;
            _options = options;
            _stream = stream;
            _blockHeadersCount = options.BlockHeaderSize / _1B;
            _cachedHeaders = BlockHeader.ToDictionary<long?>();
            _blockFirstSector = new byte[_options.DiskSectorSize];
            _originPositionInStream = Id * _options.BlockSize;

            ReadBlockFirstSector();
        }

        public long GetHeaderValue(BlockHeader header)
        {
            AssertNotDisposed();
            AssertHeaderSize(header);

            if (_cachedHeaders[header] == null)
            {
                _cachedHeaders[header] = _blockFirstSector.ToInt64(header * _1B);
            }

            return (long)_cachedHeaders[header];
        }

        public void SetHeaderValue(BlockHeader header, long value)
        {
            AssertNotDisposed();
            AssertHeaderSize(header);

            _blockFirstSector.Write(value, header * _1B);
            _cachedHeaders[header] = value;

            _isBlockFirstSectorDirty = true;
        }

        public void Read(byte[] dst, int dstOffset, int blockOffset, int count)
        {
            AssertNotDisposed();
            AssertReadArguments(dst, dstOffset, blockOffset, count);

            var bytesCopied = 0;
            var isInFirstSector = IsInFirstSector(ref blockOffset);
            if (isInFirstSector)
            {
                bytesCopied = CopyFromBlockFirstSector(dst, dstOffset, blockOffset, count);
            }

            // set stream position
            if (bytesCopied < count)
            {
                if (isInFirstSector)
                {
                    _stream.Position = _originPositionInStream + _options.DiskSectorSize;
                }
                else
                {
                    _stream.Position = _originPositionInStream + _options.BlockHeaderSize + blockOffset;
                }
            }

            ReadAllData(ref dst, ref dstOffset, ref count, ref bytesCopied);
        }

        public long ReadContent(byte[] dst, int dstOffset)
        {
            var contentLength = GetHeaderValue(BlockContentLength);
            if (contentLength > _options.BlockContentSize)
            {
                throw new InvalidDataException("Unexpected block content length: " + contentLength);
            }

            Read(
                dst: dst
              , dstOffset: dstOffset
              , blockOffset: 0
              , count: (int)contentLength);

            return contentLength;
        }

        public void Write(byte[] src, int srcOffset, int blockOffset, int count)
        {
            AssertNotDisposed();
            AssertWriteArguments(src, srcOffset, blockOffset, count);

            var bytesCopied = 0;
            var isInFirstSector = IsInFirstSector(ref blockOffset);
            if (isInFirstSector)
            {
                bytesCopied = CopyToBlockFirstSectorAfterHeaders(ref src, ref srcOffset, ref blockOffset, ref count);
            }

            var isBeyondFirstSector = IsBeyondFirstSector(ref blockOffset, ref count);
            if (isBeyondFirstSector)
            {
                _stream.Position = _originPositionInStream
                    + Math.Max(_options.DiskSectorSize, _options.BlockHeaderSize + blockOffset);

                ExcludeBytesWrittenToFirstSector(ref srcOffset, ref blockOffset, ref count, ref bytesCopied);

                WriteAllData(ref src, ref srcOffset, ref count);
            }
        }

        protected virtual void OnDisposed(EventArgs e)
        {
            Disposed?.Invoke(this, e);
        }

        private void ReadBlockFirstSector()
        {
            _stream.Position = _originPositionInStream;
            _stream.Read(_blockFirstSector, 0, _options.DiskSectorSize);
        }

        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("Block");
            }
        }

        private void AssertHeaderSize(BlockHeader header)
        {
            if (header >= _blockHeadersCount)
            {
                throw new ArgumentException($"Header type {header} exceeds block headers count.");
            }
        }

        private void AssertReadArguments(byte[] buffer, int bufferOffset, int blockOffset, int count)
        {
            if (false == ((count >= 0) && ((count + blockOffset) <= _options.BlockContentSize)))
            {
                throw new ArgumentOutOfRangeException("Requested count is outside of block size bounds: Count=" + count, "count");
            }

            if (false == ((count + bufferOffset) <= buffer.Length))
            {
                throw new ArgumentOutOfRangeException("Requested count is outside of destination buffer bounds: Count=" + count);
            }
        }

        private void AssertWriteArguments(byte[] src, int srcOffset, int blockOffset, int count)
        {
            if (false == ((blockOffset >= 0) && ((blockOffset + count) <= _options.BlockContentSize)))
            {
                throw new ArgumentOutOfRangeException("Count argument is outside of dest bounds: Count=" + count, "count");
            }

            if (false == ((srcOffset >= 0) && ((srcOffset + count) <= src.Length)))
            {
                throw new ArgumentOutOfRangeException("Count argument is outside of src bounds: Count=" + count, "count");
            }
        }

        private int CopyFromBlockFirstSector(byte[] dst, int dstOffset, int blockOffset, int count)
        {
            var bytesToCopy = Math.Min(_options.DiskSectorSize - _options.BlockHeaderSize - blockOffset, count);

            Buffer.BlockCopy(src: _blockFirstSector
                , srcOffset: _options.BlockHeaderSize + blockOffset
                , dst: dst
                , dstOffset: dstOffset
                , count: bytesToCopy);

            return bytesToCopy;
        }

        private int CopyToBlockFirstSectorAfterHeaders(ref byte[] src, ref int srcOffset, ref int blockOffset, ref int count)
        {
            var bytesToCopy = Math.Min(count, _options.DiskSectorSize - _options.BlockHeaderSize - blockOffset);

            Buffer.BlockCopy(src: src
                , srcOffset: srcOffset
                , dst: _blockFirstSector
                , dstOffset: _options.BlockHeaderSize + blockOffset
                , count: bytesToCopy);

            _isBlockFirstSectorDirty = true;

            return bytesToCopy;
        }

        private bool IsInFirstSector(ref int blockOffset)
        {
            return (_options.BlockHeaderSize + blockOffset) < _options.DiskSectorSize;
        }

        private bool IsBeyondFirstSector(ref int blockOffset, ref int count)
        {
            return (_options.BlockHeaderSize + blockOffset + count) > _options.DiskSectorSize;
        }

        private void ReadAllData(ref byte[] dst, ref int dstOffset, ref int count, ref int bytesCopied)
        {
            while (bytesCopied < count)
            {
                var bytesToRead = Math.Min(_options.DiskSectorSize, count - bytesCopied);
                var thisRead = _stream.Read(dst, dstOffset + bytesCopied, bytesToRead);
                if (thisRead == 0)
                {
                    throw new EndOfStreamException();
                }
                bytesCopied += thisRead;
            }
        }

        private void ExcludeBytesWrittenToFirstSector(ref int srcOffset, ref int blockOffset, ref int count, ref int bytesCopied)
        {
            blockOffset += bytesCopied;
            srcOffset += bytesCopied;
            count -= bytesCopied;
        }

        private void WriteAllData(ref byte[] src, ref int srcOffset, ref int count)
        {
            var bytesWritten = 0;
            while (bytesWritten < count)
            {
                var bytesToWrite = Math.Min(_options.DiskSectorSize, count - bytesWritten);
                _stream.Write(src, srcOffset + bytesWritten, bytesToWrite);
                _stream.Flush();
                bytesWritten += bytesToWrite;
            }
        }

        #region IDisposable Support
        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                if (_isBlockFirstSectorDirty)
                {
                    _stream.Position = _originPositionInStream;
                    _stream.Write(_blockFirstSector, 0, _options.DiskSectorSize);
                    _stream.Flush();
                    _isBlockFirstSectorDirty = false;
                }

                _isDisposed = true;

                OnDisposed(EventArgs.Empty);
            }
        }

        ~Block()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
