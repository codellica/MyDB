using System;
using System.IO;
using System.Linq;
using static MyDB.Engine.Record;

namespace MyDB.Engine
{
    public class RecordStorage : IRecordStorage
    {
        private readonly IBlockStorage _blockStorage;

        public RecordStorage(IBlockStorage blockStorage)
        {
            _blockStorage = blockStorage ?? throw new ArgumentNullException(nameof(blockStorage));
        }

        public long Create()
        {
            throw new NotImplementedException();
        }

        public long Create(byte[] data)
        {
            throw new NotImplementedException();
        }

        public long Create(Func<long, byte[]> dataGenerator)
        {
            throw new NotImplementedException();
        }

        public void Delete(long recordId)
        {
            throw new NotImplementedException();
        }

        public byte[] Get(long recordId)
        {
            if (recordId == RecordZero)
            {
                throw new ArgumentException("Record with id 0 is reserved.");
            }

            using (var record = GetRecord(recordId))
            {
                return record.Content;
            }
        }

        public void Update(long recordId, byte[] data)
        {
            throw new NotImplementedException();
        }

        private IRecord GetRecord(long recordId)
        {
            var record = new Record(recordId);

            try
            {
                var currentBlockId = record.Id;

                do
                {
                    var block = _blockStorage.Find(currentBlockId);
                    if (block == null)
                    {
                        if (currentBlockId == RecordZero)
                        {
                            block = _blockStorage.Create();
                            return RecordZero.WithBlocks(new[] { block });
                        }
                        else
                        {
                            throw new Exception("Block not found by id: " + currentBlockId);
                        }
                    }

                    if (block.IsDeleted)
                    {
                        throw new InvalidDataException("Block not found: " + currentBlockId);
                    }

                    record.Blocks.Add(block);

                    currentBlockId = block.GetHeaderValue(BlockHeader.NextBlock);

                } while (currentBlockId != 0);
            }
            catch (Exception)
            {
                record.Dispose();
                throw;
            }

            return record;
        }

        IBlock AllocateBlock()
        {
            var freeBlock = FindFreeBlock();

            freeBlock = freeBlock ?? _blockStorage.Create();

            if (freeBlock == null)
            {
                throw new Exception("Failed to create new block");
            }

            return freeBlock;
        }

        private IBlock FindFreeBlock()
        {
            using (var record = GetRecord(RecordZero))
            {
                long blockId = 0;
                var lastBlock = record.Blocks.Last();

                try
                {
                    blockId = lastBlock.ReadInt64FromContentTail();
                }
                catch (Exception ex)
                when (ex is InvalidDataException || ex is DataMisalignedException)
                {
                    return null;
                }

                return _blockStorage.Find(blockId);
            }
        }
    }
}
