using System;
#if DEBUG
using System.Diagnostics;
#endif

namespace Mordent.Core
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
#endif
    public readonly struct Lsn: IComparable<Lsn>, IEquatable<Lsn>
    {
        private readonly long _value;
        internal static readonly Lsn None = new Lsn(-1);

        public Lsn(long value) => _value = value;

        public int CompareTo(Lsn other) => _value.CompareTo(other._value);

        public override string ToString() => _value.ToString("x");

        public override bool Equals(object obj) => obj is Lsn other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public bool Equals(Lsn other) => _value == other._value;
        public static bool operator ==(Lsn left, Lsn right) => left.Equals(right);

        public static bool operator !=(Lsn left, Lsn right) => !(left == right);

        public static bool operator <(Lsn left, Lsn right) => left.CompareTo(right) < 0;

        public static bool operator <=(Lsn left, Lsn right) => left.CompareTo(right) <= 0;

        public static bool operator >(Lsn left, Lsn right) => left.CompareTo(right) > 0;

        public static bool operator >=(Lsn left, Lsn right) => left.CompareTo(right) >= 0;

        public static Lsn operator ++(Lsn other) => new Lsn(other._value + 1);

    }
}
