using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Pack =2, Size =2)]
    public struct DbRowId
    {
        public int PageNo;
        public ushort FileNo;
        public short SlotNo;
        public static DbRowId None = new DbRowId();

        public DbRowId(int pageNo, short slotNo) => (PageNo, FileNo, SlotNo) = (pageNo, 0, slotNo);

    }

}


