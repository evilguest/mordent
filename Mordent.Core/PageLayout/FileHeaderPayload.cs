using System;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Pack=2, Size = DbPage.Size)]
    public struct FileHeaderPayload
    {
        public const ulong MordentDataTag = 0x44_74_6E_65_64_72_6F_4D;

        public ulong Tag; 
        public uint Version; // file format version
        public uint Type; // file type
        public ulong RedoStartLSN;
        public int MaxPagesGrowth;
        public int AvailablePages;
    }
}
