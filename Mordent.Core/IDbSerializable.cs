using System;
using System.Collections.Generic;

namespace Mordent.Core
{
    public interface IDbSerializable
    {
        public short FixedDataSize { get; }
        public int TotalDataSize { get; }
        public IEnumerable<short> DataItems { get; }
        public short Write(Span<byte> space, short dataItem);
    }
}