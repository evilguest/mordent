using System;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Pack=2, Size = DbPage.Size)]
    public struct DbFileHeaderPage
    {
        public ulong Tag; // TODO: invent a unique tag
        public uint Version; // file format version
        public uint Type; // file type
        public ulong RedoStartLSN;
        public int MaxPagesGrowth;
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
            set => _data = (_data & ~DbPage.SizeMask) | (value & DbPage.SizeMask);
        }
    }



}
