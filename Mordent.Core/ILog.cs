﻿using System;
using System.Collections.Generic;

namespace Mordent.Core
{
    public readonly struct Lsn: IComparable<Lsn>, IEquatable<Lsn>
    {
        private readonly long _value;
        public Lsn(long value) => _value = value;

        public int CompareTo(Lsn other) => _value.CompareTo(other);

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

    public interface ILogFile
    {
        public Lsn Append(ReadOnlySpan<byte> data);
        public void Flush(Lsn upToLsn);
        public IEnumerable<IReadOnlyList<byte>> Records { get; }
    }

    public interface ILog
    {
        public void StartTransaction(Guid transactionId);
        public void CommitTransaction(Guid transactionId);
        public void RecordChange(Guid transactionId, object oldData, object newData);
        public void StartCheckPoint();
        public void EndCheckPoint();

    }
}