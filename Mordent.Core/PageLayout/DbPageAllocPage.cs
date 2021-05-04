using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public enum PageAllocationStatus : byte
    {
        PageAllocatedMask = 0b00000001,
        PageMixedExtentMask = 0b00000010,
        PageIsIamMask = 0b00000100,
        PageFullnessMask = 0b11110000,
        PageIsEmpty = 0b00000000,
        PageUpTo50PercentFull = 0b00010000,
        PageUpTo80PercentFull = 0b00100000,
        PageUpTo95PercentFull = 0b00110000,
        PageUpTo99PercentFull = 0b01000000,
        PageCompletelyFull = 0b01010000
    }
    [StructLayout(LayoutKind.Sequential, Size = DbPage.Size)]
    public unsafe struct DbPageAllocPage
    {
        private const int Capacity = (DbPage.Size - DbPageHeader.Size) / sizeof(PageAllocationStatus);
        public DbPageHeader _header;
        private fixed byte _pageStatus[Capacity];
        public PageAllocationStatus this[ushort pageNo]
        {
            get => (PageAllocationStatus)_pageStatus[pageNo];
            set => _pageStatus[pageNo] = (byte)value;
        }
    }
}
