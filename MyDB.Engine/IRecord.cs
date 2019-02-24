using System;
using System.Collections.Generic;
using System.Text;

namespace MyDB.Engine
{
    public interface IRecord : IDisposable
    {
        long Id { get; }

        IList<IBlock> Blocks { get; }

        byte[] Content { get; }

        IRecord WithBlocks(IEnumerable<IBlock> blocks);
    }
}
