using System;
using System.Diagnostics;

namespace Mordent.Core
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct DbTranId: IEquatable<DbTranId>, IComparable<DbTranId>
    {
        //internal static readonly DbTranId Null = new DbTranId(0);
        internal static readonly DbTranId None = new(0);
        private long _value;
        public DbTranId(long value) => _value = value;

        public int CompareTo(DbTranId other) => _value.CompareTo(other._value);

        public bool Equals(DbTranId other) => other._value == _value;

        public override bool Equals(object obj) => obj is DbTranId && Equals((DbTranId)obj);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() =>
            ((_value >> 48) & 0xFFFF).ToString("X") + ":" +
            ((_value >> 32) & 0xFFFF).ToString("X") + ":" +
            ((_value >> 16) & 0xFFFF).ToString("X") + ":" +
            (_value & 0xFFFF).ToString("X") + ":";
        private string DebuggerDisplay { get => ToString(); }

        public static bool operator ==(DbTranId left, DbTranId right) => left.Equals(right);

        public static bool operator !=(DbTranId left, DbTranId right) => !(left == right);

        public static bool operator <(DbTranId left, DbTranId right) => left.CompareTo(right) < 0;

        public static bool operator <=(DbTranId left, DbTranId right) => left.CompareTo(right) <= 0;

        public static bool operator >(DbTranId left, DbTranId right) => left.CompareTo(right) > 0;

        public static bool operator >=(DbTranId left, DbTranId right) => left.CompareTo(right) >= 0;

        public static DbTranId operator ++(DbTranId tranId) => new DbTranId(tranId._value + 1);
    }
}