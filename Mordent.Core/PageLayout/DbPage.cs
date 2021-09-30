﻿using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public enum DbPageType : ushort
    {
        FileHeader,
        FreeSpace,
        GlobalAllocationMap,
        SharedAllocationMap,
        Heap,
        Index,
        Log
    }
    [StructLayout(LayoutKind.Sequential, Size = DbPageHeader.Size)]
    public struct DbPageHeader
    {
        public const int Size = 2;
        public DbPageType Type;
    }
    public struct DbRowReference
    {
        private long _data;
        public long PageNo
        {
            get => _data >> 14;
            set => _data = _data & (DbPage.Size - 1) | value << DbPage.SizeLog;
        }
        public short RowNo
        {
            get => (short)(_data & (DbPage.Size - 1));
            set => _data = (_data & ~DbPage.SizeMask) | (value & DbPage.SizeMask);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = DbPage.Size)]
    public partial struct DbPage 
    {
        public const int SizeLog = 14;
        public const int Size = 1 << SizeLog; // 16 KB
        public const int SizeMask = Size - 1;

        [FieldOffset(0)]
        public DbPageHeader Header;

        [FieldOffset(DbPageHeader.Size)]
        public ExtentAllocPayload ExtentAlloc;

        [FieldOffset(DbPageHeader.Size)]
        public AllocPagePayload PageAlloc;

        [FieldOffset(DbPageHeader.Size)]
        public RowDataPayload RowData;

        [FieldOffset(DbPageHeader.Size)]
        public FileHeaderPayload FileHeader;
        [FieldOffset(DbPageHeader.Size)]
        public LogPayload Log;
        public void InitAsPfPage() => Header.Type = DbPageType.FreeSpace;

        public void InitAsGamPage()
        {
            Header.Type = DbPageType.GlobalAllocationMap;
            ExtentAlloc.Initialize(true);
        }
        public void InitAsSGamPage()
        {
            Header.Type = DbPageType.SharedAllocationMap;
            ExtentAlloc.Initialize(false);
        }
        internal void InitAsFileHeaderPage()
        {
            Header.Type = DbPageType.FileHeader;
            FileHeader.Tag = FileHeaderPayload.MordentDataTag; // Mordent
            FileHeader.Version = 0x0000_0001;
            FileHeader.Type = 42; // data file
            FileHeader.MaxPagesGrowth = int.MaxValue;
            FileHeader.RedoStartLSN = 8;
        }
        internal void InitAsLogPage()
        {
            Header.Type = DbPageType.Log;
            Log.Reset();
        }
}


}
