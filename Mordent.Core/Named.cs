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
        public override short Write(Span<byte> space, short dataItem)
        {
            switch (dataItem)
            {
                case 1:
                    MemoryMarshal.Cast<byte, int>(space)[0] = Name.Length;
                    var charSpace = MemoryMarshal.Cast<byte, char>(space.Slice(sizeof(int)));
                    Name.AsSpan().CopyTo(charSpace);
                    return (short)StringSize(Name);
                default: return base.Write(space, dataItem);
            }
        }
    }
}
