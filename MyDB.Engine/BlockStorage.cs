using System;
using System.Collections.Generic;
using System.IO;

namespace MyDB.Engine
{
    public class BlockStorage : IBlockStorage
    {
        private readonly Stream _stream;
        private readonly BlockOptions _options;
        readonly Dictionary<long, Block> _cache = new Dictionary<long, Block>();

        public BlockStorage(Stream stream, BlockOptions options)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IBlock Find(long blockId)
        {
            if (true == _cache.ContainsKey(blockId))
            {
                return _cache[blockId];
            }

            var blockPosition = blockId * _options.BlockSize;
            bool positionExceedsStreamLength = (blockPosition + _options.BlockSize) > _stream.Length;
            if (positionExceedsStreamLength)
            {
                return null;
            }

            return BlockFromStream(blockId);
        }

        public IBlock Create()
        {
            bool isStreamLengthAlignedToBlockSize = (_stream.Length % _options.BlockSize) != 0;
            if (!isStreamLengthAlignedToBlockSize)
            {
                throw new DataMisalignedException("Unexpected length of the stream: " + _stream.Length);
            }

            var blockId = CalculateNextBlockId();
            ExtendStreamByANewBlock(blockId);

            return BlockFromStream(blockId);
        }

        private void ExtendStreamByANewBlock(long blockId)
        {
            _stream.SetLength(blockId * _options.BlockSize + _options.BlockSize);
            _stream.Flush();
        }

        protected virtual void OnBlockInitialized(Block block)
        {
            _cache[block.Id] = block;
            block.Disposed += HandleBlockDisposed;
        }

        private IBlock BlockFromStream(long blockId)
        {
            var block = new Block(blockId, _options, _stream);
            OnBlockInitialized(block);
            return block;
        }

        private void HandleBlockDisposed(object sender, EventArgs e)
        {
            var block = (Block)sender;
            block.Disposed -= HandleBlockDisposed;

            _cache.Remove(block.Id);
        }

        private long CalculateNextBlockId()
        {
            return (long)Math.Ceiling(_stream.Length / (double)_options.BlockSize);
        }
    }
}
