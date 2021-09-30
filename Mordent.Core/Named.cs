using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public class Named<F> : Extend<F> where F : unmanaged
    {
        public string Name { get; set; }
        public override IEnumerable<short> DataItems => base.DataItems.Append((short)1);
        private int VariableDataSize => StringSize(Name);

        public override int TotalDataSize => base.TotalDataSize + VariableDataSize;
        public override short Write(Span<byte> space, short dataItem, IDbPageManager allocator)
        {
            switch (dataItem)
            {
                case 1:
                    return allocator.WriteString(space, Name);
                default: return base.Write(space, dataItem, allocator);
            }
        }
    }
}
