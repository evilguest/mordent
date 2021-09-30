using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    [DebuggerDisplay("{FileNo}:{PageNo}:{SlotNo}")]
    public readonly struct DbRowId
    {
        public readonly int PageNo;
        public readonly ushort FileNo;
        public readonly short SlotNo;
        public static DbRowId None = new DbRowId();
        public static DbRowId NotInit = new DbRowId(0, -1, 0);

        public DbPageId PageId => new DbPageId(FileNo, PageNo);

        public DbRowId(DbPageId pageId, short slotNo) : this(pageId.FileNo, pageId.PageNo, slotNo) { }

        public DbRowId(ushort fileNo, int pageNo, short slotNo) => (PageNo, FileNo, SlotNo) = (pageNo, fileNo, slotNo);
        public override string ToString() => $"{FileNo}:{PageNo}:{SlotNo}";
    }
}
