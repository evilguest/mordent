using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public partial struct DbPage {
        public void InitAsHeap()
        {
            Header.Type = DbPageType.Heap;
            RowData.Header.DataCount = 0;
            RowData.Header.NextPageId = DbPageId.None;
            RowData.Header.PrevPageId = DbPageId.None;
        }



        [StructLayout(LayoutKind.Sequential, Pack = 2, Size = DataPageHeader.Size)]
        public struct DataPageHeader
        {
            public const int Size = 10;
            public short DataCount;
            public DbPageId PrevPageId;
            public DbPageId NextPageId;
        }
        /// <summary>
        /// This represents the row data page. At the end of the page there is a list of 
        /// row offsets in reverse order, i.e. row 0 does have an offset at the very end.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct RowDataPayload
        {
            internal const short Capacity = (DbPage.Size - DbPageHeader.Size - DataPageHeader.Size);
            internal const short MaxSlots = Capacity / sizeof(short);

            [FieldOffset(0)]
            public DataPageHeader Header;

            [FieldOffset(DbPageHeader.Size)]
            internal fixed short _rowOffsets[MaxSlots];
            public Span<short> RowOffsets => MemoryMarshal.CreateSpan(ref _rowOffsets[0], MaxSlots);

            [FieldOffset(DbPageHeader.Size)]
            internal byte _data;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal short GetSlotOffset(short slotNo)
            {
                return slotNo == 0 ? slotNo : RowOffsets[^slotNo];
            }
            public Span<byte> GetSlotSpan(short slotNo)
            {
                fixed (byte* dataPtr = &_data)
                {
                    var s = (slotNo < Header.DataCount)
                        ? new Span<byte>(dataPtr, GetSlotOffset((short)(slotNo + 1)))
                        : new Span<byte>(dataPtr, Capacity - (Header.DataCount * sizeof(short)));
                    return s.Slice(GetSlotOffset(slotNo));
                }
            }
            public unsafe void* GetSlotPtr(short slotNo)
            {
                fixed (byte* dataPtr = &_data)
                    return dataPtr + GetSlotOffset(slotNo);
            }
            public ref T GetSlotAs<T>(short slotNo) where T : unmanaged
            {
                fixed (byte* dataPtr = &_data)
                    return ref *(T*)(dataPtr + GetSlotOffset(slotNo));
            }
            public short AddSlot(short slotSize)
            {
                // TODO: locks!
                Debug.Assert(slotSize + 2 <= FreeSpace, $"An attempt to store row with size {slotSize} in the page with free space of {FreeSpace}");
                var rowNo = Header.DataCount++;
                var offset = GetSlotOffset(rowNo);
                RowOffsets[^(rowNo+1)] = (short)(offset + slotSize);
                return rowNo;
            }
            public short FreeSpace => Header.DataCount == 0
                    ? Capacity
                    : (short)(Capacity - (Header.DataCount * sizeof(short)) - RowOffsets[^(Header.DataCount+1)]);

            public void RemoveRow(short slotNo)
            {
                // TODO: locks!
                if (slotNo + 1 < Header.DataCount) // not the last row
                {
                    var rowOffset = GetSlotOffset(slotNo);
                    var nextRowOffset = RowOffsets[^(slotNo + 2)];

                    fixed (byte* dataPtr = &_data)
                        Buffer.MemoryCopy(dataPtr + nextRowOffset, dataPtr + rowOffset, FreeSpace - rowOffset, GetSlotOffset(Header.DataCount) - nextRowOffset);

                    for (int i = slotNo + 1; i < Header.DataCount; i++)
                        RowOffsets[^(i+1)] = (short)(RowOffsets[^(i + 2)] + rowOffset - nextRowOffset);
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
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct VariablePartHeader
        {
            public ushort VariableFields;
            public VariableFieldOffset Offset0;
        }

    }
}

