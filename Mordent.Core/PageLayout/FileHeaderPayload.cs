using System;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public partial struct DbPage
    {
        public void InitAsFileHeaderPage()
        {
            Header.Type = DbPageType.FileHeader;
            FileHeader.Tag = FileHeaderPayload.MordentDataTag; // Mordent
            FileHeader.Version = 0x0000_0001;
            FileHeader.Type = 42; // data file
            FileHeader.MaxPagesGrowth = int.MaxValue;
            FileHeader.RedoStartLSN = new(0);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2, Size = DbPage.Size)]
        public struct FileHeaderPayload
        {
            public const ulong MordentDataTag = 0x44_74_6E_65_64_72_6F_4D;

            public ulong Tag;
            public uint Version; // file format version
            public uint Type; // file type
            public Lsn  RedoStartLSN;
            public int MaxPagesGrowth;
            public int AvailablePages;
        }
    }
}
