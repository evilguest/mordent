using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public enum DbPageType : ushort
    {
        FileHeader,
        FreeSpace,
        GlobalAllocationMap,
        SharedAllocationMap,
        Heap,
        Index
    }
    [StructLayout(LayoutKind.Sequential, Size = DbPageHeader.Size)]
    public struct DbPageHeader
    {
        public const int Size = 20;
        public DbPageType Type;
        public short DataCount;
        public long PrevPageNo;
        public long NextPageNo;
    }
    [StructLayout(LayoutKind.Sequential, Size = DbPage.Size)]
    public struct DbPage
    {
        public const int SizeLog = 14;
        public const int Size = 1 << SizeLog;
    }


}
