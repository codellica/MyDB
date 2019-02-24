using System;

namespace MyDB.Engine
{
    public interface IBlock : IDisposable
    {
       long Id { get; }

        bool IsDeleted { get; }

        bool IsFirst { get; }

        long GetHeaderValue(BlockHeader header);

        void SetHeaderValue(BlockHeader header, long value);

        void Read(byte[] dst, int dstOffset, int blockOffset, int count);

        long ReadContent(byte[] dst, int dstOffset);

        void Write(byte[] src, int srcOffset, int blockOffset, int count);
    }
}
