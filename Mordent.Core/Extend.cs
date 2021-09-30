using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Mordent.Core
{
    public class Extend<F> : IDbSerializable where F : unmanaged
    {
        private F _fixedData;

        public short FixedDataSize => (short)Marshal.SizeOf<F>();
        public short MinDataSize => FixedDataSize;

        public virtual int TotalDataSize => FixedDataSize;

        public virtual IEnumerable<short> DataItems => Array.Empty<short>();

        public unsafe virtual short Write(Span<byte> space, short dataItem, IDbPageManager allocator)
        {
            switch (dataItem)
            {
                case 0:
                    fixed (void* dataPtr = &_fixedData)
                        new ReadOnlySpan<byte>(dataPtr, FixedDataSize).CopyTo(space);
                    return FixedDataSize;
                default: throw new ArgumentOutOfRangeException(nameof(dataItem), "Trying to save an inexistent item");
            }
        }
        public ref F Value => ref _fixedData;
        public static int StringSize(string s) => 2 + Encoding.UTF8.GetByteCount(s);
        public int GetDataItemSize(short dataItem)
        {
            switch (dataItem)
            {
                case 0:
                    return FixedDataSize;
                default: throw new ArgumentOutOfRangeException(nameof(dataItem), "Trying to access an inexistent item");
            }
        }
    }
}
