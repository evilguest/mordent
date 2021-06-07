using System.Runtime.InteropServices;

namespace Mordent.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 2, Size = 12)]
    public struct StringHeader
    {
        public int Length;
        public StringSegmentHeader FirstSegmentHeader;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 8)]
    public struct StringSegmentHeader//: IEquatable<StringSegmentHeader>
    {
        [FieldOffset(0)]
        private ulong allData;
        [FieldOffset(0)]
        public int PageNo;
        [FieldOffset(4)]
        public ushort FileNo;
        [FieldOffset(6)]
        public short SegmentLen;
        public bool HasMore { get => PageNo != 0; }
        //public static readonly StringSegmentHeader Null = new();
        //public static bool operator ==(StringSegmentHeader left, StringSegmentHeader right) => left.allData == right.allData;
        //public static bool operator !=(StringSegmentHeader left, StringSegmentHeader right) => left.allData != right.allData;
        //public override bool Equals(object obj) => obj is StringSegmentHeader ssh && ssh == this;
        //public override int GetHashCode() => PageNo ^ SegmentLen ^ (FileNo << 16);

        //public bool Equals(StringSegmentHeader other) => allData == other.allData;
    }

}
