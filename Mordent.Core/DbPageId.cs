using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct DbPageId : IEquatable<DbPageId>
    {
        public const int Size = sizeof(int) + sizeof(ushort);
        public readonly int PageNo;
        public readonly ushort FileNo;
        public static DbPageId None = new DbPageId();
        public static DbPageId NotInit = new DbPageId(0, -1);


        public DbPageId(ushort fileNo, int pageNo) => (PageNo, FileNo) = (pageNo, fileNo);

        public bool Equals(DbPageId other) => other.PageNo == PageNo && other.FileNo == FileNo;

        public override bool Equals(object obj) => obj is DbPageId && Equals((DbPageId)obj);

        public override int GetHashCode() => HashCode.Combine(PageNo, FileNo);

        public static bool operator ==(DbPageId left, DbPageId right) => left.Equals(right);

        public static bool operator !=(DbPageId left, DbPageId right) => !(left == right);
        //public static implicit operator DbPageId((int fileNo, int pageNo) t) => new DbPageId((ushort)t.fileNo, t.pageNo);
        //public static implicit operator DbPageId((ushort fileNo, int pageNo) t) => new DbPageId(t.fileNo, t.pageNo);
        public override string ToString() => $"{FileNo}:{PageNo}";
    }
}
