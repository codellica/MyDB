using System;
using static MyDB.Engine.ByteNumber;

namespace MyDB.Engine
{
    public static class ByteExtensions
    {
        public static long ToInt64(this byte[] buffer, int bufferOffset)
        {
            var value = new byte[_1B];
            Buffer.BlockCopy(buffer, bufferOffset, value, 0, _1B);
            return BitConverter.ToInt64(value);
        }

		public static void Write(this byte[] buffer, long value, int bufferOffset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, bufferOffset, _1B);
		}
    }
}
