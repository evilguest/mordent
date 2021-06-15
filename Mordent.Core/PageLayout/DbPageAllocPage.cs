using System;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [Flags]
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
    public unsafe struct DbPageAllocPage: IDbPage
    {
        public const int PagesCapacity = (DbPage.Size - DbPageHeader.Size) / sizeof(PageAllocationStatus);
        public DbPageHeader _header;
        private fixed byte _pageStatus[PagesCapacity];
        public PageAllocationStatus this[ushort pageNo]
        {
            get => (PageAllocationStatus)_pageStatus[pageNo];
            set => _pageStatus[pageNo] = (byte)value;
        }

        public ref DbPageHeader Header // we inject header into every page.
        {
            get
            {
                fixed (DbPageHeader* headerPtr = &_header)
                    return ref *headerPtr;
            }
        }


        /// <summary>
        /// Looks through eight pages in the requested extent to find the unallocated one.
        /// </summary>
        /// <param name="extentNumber">The number of extent to scan</param>
        /// <returns>The number (0 to 7) of the page within extent</returns>
        public int FindFirstNonAllocatedPage(int extentNumber)
        {
            fixed (DbPageAllocPage* pagePtr = &this)
                return new ReadOnlySpan<byte>(pagePtr->_pageStatus + extentNumber * 8, 8).IndexOf((byte)0);
        }
    }
}
