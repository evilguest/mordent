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
        public int PrevPageNo;
        public int NextPageNo;
    }
    [StructLayout(LayoutKind.Sequential, Size = DbPage.Size)]
    public unsafe struct DbPage : IDbPage
    {
        public const int SizeLog = 14;
        public const int Size = 1 << SizeLog; // 16 KB
        public const int SizeMask = Size - 1;

        private DbPageHeader _header;
        //        private fixed byte _fill[Size - DbPageHeader.Size];

        public ref DbPageHeader Header // we inject header into every page.
        {
            get
            {
                fixed (DbPageHeader* headerPtr = &_header)
                    return ref *headerPtr;
            }
        } 
    }


}
