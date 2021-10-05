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
            public const int Size = sizeof(ushort) + DbPageId.Size + DbPageId.Size;
            public ushort DataCount;
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
            public const ushort Capacity = (DbPage.Size - DbPageHeader.Size - DataPageHeader.Size);
            public const ushort MaxSlots = Capacity / sizeof(short);

            [FieldOffset(0)]
            public DataPageHeader Header;

            [FieldOffset(DbPageHeader.Size)]
            internal fixed ushort _rowOffsets[MaxSlots];
            public Span<ushort> RowOffsets => MemoryMarshal.CreateSpan(ref _rowOffsets[0], MaxSlots);

            [FieldOffset(DbPageHeader.Size)]
            internal byte _data;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ushort GetSlotOffset(ushort slotNo)
            {
                return slotNo == 0 ? slotNo : RowOffsets[^slotNo];
            }
            public Span<byte> GetSlotSpan(ushort slotNo)
            {
                fixed (byte* dataPtr = &_data)
                {
                    var s = (slotNo < Header.DataCount)
                        ? new Span<byte>(dataPtr, GetSlotOffset((ushort)(slotNo + 1)))
                        : new Span<byte>(dataPtr, Capacity - (Header.DataCount * sizeof(short)));
                    return s.Slice(GetSlotOffset(slotNo));
                }
            }
            //public unsafe void* GetSlotPtr(ushort slotNo)
            //{
            //    fixed (byte* dataPtr = &_data)
            //        return dataPtr + GetSlotOffset(slotNo);
            //}
            //public ref T GetSlotAs<T>(ushort slotNo) where T : unmanaged
            //{
            //    fixed (byte* dataPtr = &_data)
            //        return ref *(T*)(dataPtr + GetSlotOffset(slotNo));
            //}
            public ushort AddSlot(ushort slotSize)
            {
                if (slotSize > FreeSpace)
                    throw new ArgumentOutOfRangeException(nameof(slotSize), slotSize, $"Attempt to allocate {slotSize} bytes at a page with {FreeSpace} bytes available");
                var rowNo = Header.DataCount++;
                var offset = GetSlotOffset(rowNo);
                RowOffsets[^(rowNo+1)] = (ushort)(offset + slotSize);
                return rowNo;
            }
            public short FreeSpace
            {
                get
                {
                    var freeSpace = Capacity - Header.DataCount * sizeof(ushort);
                    if (Header.DataCount > 0)
                        freeSpace -= RowOffsets[^(Header.DataCount)];
                     return (short)(freeSpace - sizeof(short));
                }
            }

            public void RemoveRow(ushort slotNo)
            {
                // TODO: locks!
                if (slotNo.Add(1) < Header.DataCount) // not the last row
                {
                    var rowOffset = GetSlotOffset(slotNo);
                    var nextRowOffset = GetSlotOffset(slotNo.Add(1));

                    fixed (byte* dataPtr = &_data)
                        Buffer.MemoryCopy(dataPtr + nextRowOffset, dataPtr + rowOffset, FreeSpace - rowOffset, GetSlotOffset(Header.DataCount) - nextRowOffset);

                    for (int i = slotNo + 1; i < Header.DataCount; i++)
                        RowOffsets[^i] = (ushort)(RowOffsets[^(i + 1)] + rowOffset - nextRowOffset);
                    Header.DataCount--;
                }
                else if (slotNo.Add(1) == Header.DataCount) // the last row
                    Header.DataCount--;
                else
                    throw new ArgumentOutOfRangeException(nameof(slotNo), slotNo, $"Trying to remove record # {slotNo + 1} of {Header.DataCount}");
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

