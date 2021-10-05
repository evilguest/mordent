using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    [DebuggerDisplay("{DebuggerDisplay, nq")]
    public readonly struct DbRowId
    {
        public readonly int PageNo;
        public readonly ushort FileNo;
        public readonly ushort SlotNo;
        public static DbRowId None = new DbRowId();
        public static DbRowId NotInit = new DbRowId(0, -1, 0);

        public DbPageId PageId => new DbPageId(FileNo, PageNo);

        public DbRowId(DbPageId pageId, ushort slotNo) : this(pageId.FileNo, pageId.PageNo, slotNo) { }

        public DbRowId(ushort fileNo, int pageNo, ushort slotNo) => (PageNo, FileNo, SlotNo) = (pageNo, fileNo, slotNo);
        [ExcludeFromCodeCoverage]
        private string DebuggerDisplay => ToString();
        public override string ToString() => $"{FileNo}:{PageNo}:{SlotNo}";
    }
}
