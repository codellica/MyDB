using System;
using System.Collections.Generic;
using System.Linq;

namespace MyDB.Engine
{
    public class BlockHeader
    {
        protected int index;
        protected string name;
        protected static readonly IDictionary<int, BlockHeader> values = new Dictionary<int, BlockHeader>();

        public static readonly BlockHeader NextBlock = new BlockHeader(0, "NextBlock");
        public static readonly BlockHeader PreviousBlock = new BlockHeader(1, "PreviousBlock");
        public static readonly BlockHeader RecordLength = new BlockHeader(2, "RecordLength");
        public static readonly BlockHeader BlockContentLength = new BlockHeader(3, "BlockContentLength");
        public static readonly BlockHeader BlockDeleted = new BlockHeader(4, "BlockDeleted");

        protected BlockHeader(int index, string name)
        {
            this.index = index;
            this.name = name;
            values.Add(index, this);
        }

        public static implicit operator int(BlockHeader header) => header.index;

        public static implicit operator BlockHeader(int index) => values.TryGetValue(index, out var header) ? header : null;

        public static implicit operator string(BlockHeader header) => header?.ToString();

        public static implicit operator BlockHeader(string name) 
            => name == null ? null 
               : values.Values.FirstOrDefault(item => name.Equals(item.name, StringComparison.CurrentCultureIgnoreCase));

        public override string ToString() => $"{index}: {name}";

        public static IDictionary<int, TValue> ToDictionary<TValue>() 
            => values.Values.ToDictionary(x => x.index, x => default(TValue));
    }
}
