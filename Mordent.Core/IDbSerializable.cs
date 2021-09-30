using System;
using System.Collections.Generic;

namespace Mordent.Core
{
    public interface IDbSerializable
    {
        public short FixedDataSize { get; }
        public short MinDataSize { get; }
        public int TotalDataSize { get; }
        public IEnumerable<short> DataItems { get; }
        public int GetDataItemSize(short dataItem);
        public short Write(Span<byte> space, short dataItem, IDbPageManager allocator);
        //public short Read(ReadOnlySpan<byte> span, short dataItem);
    }
}