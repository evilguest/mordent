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
        Index,
        Log
    }
    [StructLayout(LayoutKind.Sequential, Size = DbPageHeader.Size)]
    public struct DbPageHeader
    {
        public const int Size = 2;
        public DbPageType Type;
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
        public PageAllocPayload PageAlloc;

        [FieldOffset(DbPageHeader.Size)]
        public RowDataPayload RowData;

        [FieldOffset(DbPageHeader.Size)]
        public FileHeaderPayload FileHeader;
        [FieldOffset(DbPageHeader.Size)]
        public LogPayload Log;

        public void InitAsLogPage()
        {
            Header.Type = DbPageType.Log;
            Log.Reset();
        }
}


}
