using System;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Size = DbPage.Size)]
    public unsafe struct DbExtentAllocPage
    {
        private const int Capacity = (DbPage.Size - DbPageHeader.Size) / sizeof(PageAllocationStatus);
        public const int ExtentsCount = Capacity * 8; // bits per byte
        public DbPageHeader Header;
        internal fixed byte _extentStatus[Capacity];
        /// <summary>
        /// Reports whether the requested extent is free
        /// </summary>
        /// <param name="extentIndex">an index of extent to query</param>
        /// <returns></returns>
        public bool this[int extentIndex]
        {
            get => (_extentStatus[extentIndex >> 3] & (1 << extentIndex & 0b111)) != 0;
            set
            {
                if (value)
                    _extentStatus[extentIndex >> 3] |= (byte)(1 << extentIndex & 0b111);
                else
                    _extentStatus[extentIndex >> 3] &= (byte)~(1 << extentIndex & 0b111);

            }
        }

        public unsafe int FindFirstFreeExtent()
        {
            fixed (DbExtentAllocPage* pagePtr = &this)
                return _hardwareHelper.FindFirstFreeExtent(pagePtr);
        }
        public unsafe void Initialize(bool value)
        {
            fixed (DbExtentAllocPage* pagePtr = &this)
                _hardwareHelper.Initialize(pagePtr, value);
        }

        private static readonly IDatabaseHardware _hardwareHelper = InitializeHardware();

        private static IDatabaseHardware InitializeHardware()
        {
            // here is a stupid slow version.
            return new BaselineDatabaseHardware();
            // TODO: replace with SIMD
        }

        private unsafe class BaselineDatabaseHardware : IDatabaseHardware
        {
            public int FindFirstFreeExtent(DbExtentAllocPage* pagePtr)
            {
                for (int i = 0; i < Capacity; i++)
                {
                    var b = pagePtr->_extentStatus[i];
                    if (b > 0) // a-ha! Some 1's are there. Let's find the first one
                    {
                        var j = 0;
                        while ((b & (1 << j)) == 0)
                            j++;
                        return i << 3 + j;
                    }
                }
                return -1;
            }

            public void Initialize(DbExtentAllocPage* pagePtr, bool value)
            {
                if (value)
                    for (int i = 0; i < Capacity; i++)
                        pagePtr->_extentStatus[i] = 0xFF;
                else
                    for (int i = 0; i < Capacity; i++)
                        pagePtr->_extentStatus[i] = 0x00;
            }
        }
    }
    

    public unsafe interface IDatabaseHardware
    {
        /// <summary>
        /// Scans through the extent map and locates the position of the first free extent
        /// </summary>
        /// <returns>The number of the first free extent (zero-based) or -1 if there are no free extents </returns>
        public int FindFirstFreeExtent(DbExtentAllocPage * pagePtr);
        /// <summary>
        /// Initializes the page to all ones 
        /// </summary>
        /// <param name="pagePtr">Pointer to the page to initialize</param>
        public void Initialize(DbExtentAllocPage* pagePtr, bool value);
    }

}