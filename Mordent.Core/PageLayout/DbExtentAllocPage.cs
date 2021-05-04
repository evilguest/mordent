using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Size = DbPage.Size)]
    public unsafe struct DbExtentAllocPage
    {
        private const int Capacity = (DbPage.Size - DbPageHeader.Size) / sizeof(PageAllocationStatus);
        public DbPageHeader _header;
        private fixed byte _extentStatus[Capacity];
        public bool this[int extentIndex]
        {
            get => (_extentStatus[extentIndex >> 3] & (1 << extentIndex & 0b111))!=0 ;
            set
            {
                if (value)
                    _extentStatus[extentIndex >> 3] |= (byte) (1 << extentIndex & 0b111);
                else
                    _extentStatus[extentIndex >> 3] &= (byte) ~(1 << extentIndex & 0b111);

            }
        }
    }
}
