using System;
using static MyDB.Engine.ByteNumber;

namespace MyDB.Engine
{
    public class BlockOptions
    {
        public int BlockHeaderSize { get; }

        public int BlockSize { get; }

        public int BlockContentSize => BlockSize - BlockHeaderSize;

        public int DiskSectorSize { get; }

        public BlockOptions(int blockSize = _4KB, int blockHeaderSize = _6B)
        {
            if (blockHeaderSize >= blockSize) 
            {
				throw new ArgumentException (
                    $"{nameof(blockHeaderSize)} cannot be larger than or equal to {nameof(blockSize)}.");
			}

			if (blockSize < _16B) 
            {
				throw new ArgumentException ($"{nameof(blockSize)} is too small.");
			}

            BlockSize = blockSize;
            BlockHeaderSize = blockHeaderSize;
            DiskSectorSize = (blockSize >= _4KB) ? _4KB : _16B;
        }
    }
}
