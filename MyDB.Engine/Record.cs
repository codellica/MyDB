using System;
using System.Collections.Generic;
using System.Linq;
using static MyDB.Engine.BlockHeader;
using static MyDB.Engine.ByteNumber;

namespace MyDB.Engine
{
    public class Record : IRecord
    {
        public const int MaxRecordSize = _4MB;
        public static readonly Record RecordZero = new Record(0);

        public long Id { get; }

        public IList<IBlock> Blocks { get; }

        public byte[] Content => GetContent();

        public Record(long recordId)
        {
            Id = recordId;
            Blocks = new List<IBlock>();
        }

        public Record(long recordId, IEnumerable<IBlock> blocks)
        {
            Id = recordId;
            Blocks = new List<IBlock>(blocks);
        }

        public IRecord WithBlocks(IEnumerable<IBlock> blocks)
        {
            return new Record(Id, blocks);
        }

        public bool Equals(Record record)
        {
            return record != null && record.Id == Id;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Record);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static implicit operator long(Record record) => record.Id;

        public static implicit operator Record(long recordId) => new Record(recordId);

        #region IDisposable Support
        private bool _isDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                foreach (var block in Blocks)
                {
                    block.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~Record()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private byte[] GetContent()
        {
            if (Blocks.Count == 0)
            {
                throw new InvalidOperationException("Cannot read content because record is empty.");
            }

            var recordSize = Blocks.First().GetHeaderValue(RecordLength);
            if (recordSize > MaxRecordSize)
            {
                throw new NotSupportedException("Unexpected record length: " + recordSize);
            }

            int bytesRead = 0;
            var data = new byte[recordSize];

            foreach (var block in Blocks)
            {
               bytesRead += (int)block.ReadContent(dst: data, dstOffset: bytesRead);
            }

            return data;
        }

    }
}
