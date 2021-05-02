using System;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Size = DbPage.Size)]
    public ref struct DbFileHeaderPage
    {
        public ulong Tag; // TODO: invent a unique tag
        public uint Version; // file format version
        public uint Type; // file type
        public int MaxPagesGrowth;
        public ulong RedoStartLSN;
    }

    public struct DbRowReference
    {
        private long _data;
        public long PageNo 
        { 
            get => _data >> 14; 
            set => _data = _data & (DbPage.Size-1) | value << DbPage.SizeLog; 
        }
        public short RowNo 
        { 
            get => (short)(_data & (DbPage.Size - 1));
            set => _data = (_data & ~(DbPage.Size - 1)) | (value & (DbPage.Size - 1));
        }
    }
    public enum DbPageType: ushort
    {
        FileHeader,
        FreeSpace,
        GlobalAllocationMap,
        SharedAllocationMap,
        Heap,
        Index
    }
    [StructLayout(LayoutKind.Sequential, Size=DbPageHeader.Size)]
    public ref struct DbPageHeader
    {
        public const int Size = 20;
        public DbPageType Type;
        public long PrevPageNo;
        public long NextPageNo;
    }


    public enum PageAllocationStatus: byte
    {
        PageAllocatedMask     = 0b00000001,
        PageMixedExtentMask   = 0b00000010,
        PageIsIamMask         = 0b00000100,
        PageFullnessMask      = 0b11110000,
        PageIsEmpty           = 0b00000000,
        PageUpTo50PercentFull = 0b00010000,
        PageUpTo80PercentFull = 0b00100000,
        PageUpTo95PercentFull = 0b00110000,
        PageUpTo99PercentFull = 0b01000000,
        PageCompletelyFull    = 0b01010000
    }
    [StructLayout(LayoutKind.Sequential, Size = DbPage.Size)]
    public unsafe ref struct DbFreeSpacePage
    {
        DbPageHeader header;
        private fixed byte _pageStatus[DbPage.Size-DbPageHeader.Size]; 
        public PageAllocationStatus this[ushort pageNo]
        {
            get => (PageAllocationStatus)_pageStatus[pageNo];
            set => _pageStatus[pageNo] = (byte)value;
        }
    }

}
