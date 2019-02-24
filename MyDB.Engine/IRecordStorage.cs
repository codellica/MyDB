using System;
using System.Collections.Generic;
using System.Text;

namespace MyDB.Engine
{
    public interface IRecordStorage
    {
        void Update(long recordId, byte[] data);

        byte[] Get(long recordId);

        long Create();

        long Create(byte[] data);

        long Create(Func<long, byte[]> dataGenerator);

        void Delete(long recordId);
    }
}
