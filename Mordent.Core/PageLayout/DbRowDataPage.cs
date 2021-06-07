using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    /// <summary>
    /// This represents the row data page. At the end of the page there is a list of 
    /// row offsets in reverse order, i.e. row 0 does have an offset at the very end.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = DbPage.Size)]
    public unsafe struct DbRowDataPage
    {
        internal const short Capacity = (DbPage.Size - DbPageHeader.Size);
        internal const short MaxSlots = Capacity / sizeof(short);
        [FieldOffset(0)]
        public DbPageHeader Header;
        [FieldOffset(DbPageHeader.Size)]
        internal fixed short _rowOffsets[MaxSlots];
        [FieldOffset(DbPageHeader.Size)]
        internal byte _data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal short GetSlotOffset(short slotNo)
        {
            Debug.Assert(slotNo <= Header.DataCount, $"Row access mismatch. Getting row #{slotNo} of {Header.DataCount}");
            if (slotNo > 0)
                return _rowOffsets[MaxSlots - slotNo];
            else
                return 0;
        }
        public unsafe void* GetSlotPtr(short slotNo)
        {
            fixed (byte* dataPtr = &_data)
                return dataPtr + GetSlotOffset(slotNo);
        }
        public ref T GetSlotAs<T>(short slotNo) where T : unmanaged
        {
            fixed(byte* dataPtr = &_data)
                return ref *(T*)(dataPtr + GetSlotOffset(slotNo));
        }
        public short AddSlot(short slotSize)
        {
            // TODO: locks!
            Debug.Assert(slotSize + 2 <= FreeSpace, $"An attempt to store row with size {slotSize} in the page with free space of {FreeSpace}");
            var rowNo = Header.DataCount++;
            var offset = GetSlotOffset(rowNo);
            _rowOffsets[MaxSlots - rowNo] = (short)(offset + slotSize);
            return rowNo;
        }
        public short FreeSpace => Header.DataCount == 0
                ? Capacity
                : (short)(Capacity - (Header.DataCount * sizeof(short)) - _rowOffsets[MaxSlots - Header.DataCount]);
        public void DeleteRow(short rowNo)
        {
            // TODO: locks!
            if (rowNo+1<Header.DataCount) // not the last row
            {
                var rowOffset = GetSlotOffset(rowNo);
                var nextRowOffset = _rowOffsets[MaxSlots - rowNo - 1];

                fixed (byte* dataPtr = &_data)
                    Buffer.MemoryCopy(dataPtr + nextRowOffset, dataPtr + rowOffset, FreeSpace - rowOffset, GetSlotOffset(Header.DataCount) - nextRowOffset);

                for (int i = rowNo + 1; i < Header.DataCount; i++)
                    _rowOffsets[MaxSlots - i] = (short)(_rowOffsets[MaxSlots - i - 1] + rowOffset - nextRowOffset);
                Header.DataCount--;
            }
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 2, Size = 4)]
    public struct VariableFieldOffset
    {
        public ushort FieldNo;
        public ushort FieldOffset;
    }
    /// Row data structure:
    /// 1. All the fixed-size fields (non-nullable ones)
    /// 2. Variable-length header:
    /// 2.1 Header Len (number of fields)
    /// 2.2. Array of fieldNo:fieldOffset len of Len
    /// 
    [StructLayout(LayoutKind.Sequential, Pack =2)]
    public struct VariablePartHeader
    {
        public ushort VariableFields;
        public VariableFieldOffset Offset0;
    }

}


