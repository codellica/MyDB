using System;
using System.IO;
using static MyDB.Engine.BlockHeader;
using static MyDB.Engine.ByteNumber;

namespace MyDB.Engine
{
    public static class BlockExtensions
    {
        public static long ReadInt64FromContentTail(this IBlock block)
        {
            var buffer = new byte[_1B];
            var contentLength = block.GetHeaderValue(BlockContentLength);

            if ((contentLength % _1B) != 0)
            {
                throw new DataMisalignedException("Block content length not aligned to byte: " + contentLength);
            }

            if (contentLength == 0)
            {
                throw new InvalidDataException("Trying to dequeue Int64 from an empty block");
            }

            block.Read(
                dst: buffer
              , dstOffset: 0
              , blockOffset: (int)contentLength - _1B
              , count: _1B);

            return BitConverter.ToInt64(buffer);
        }

        public static void AppendInt64ToContent(this IBlock block, long value)
        {
            var contentLength = block.GetHeaderValue(BlockContentLength);

            if ((contentLength % _1B) != 0)
            {
                throw new DataMisalignedException("Block content length not aligned to byte: " + contentLength);
            }

            block.Write(
                src: BitConverter.GetBytes(value)
              , srcOffset: 0
              , blockOffset: (int)contentLength
              , count: _1B);
        }
    }
}
