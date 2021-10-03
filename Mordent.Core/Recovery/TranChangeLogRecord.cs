using System;

namespace Mordent.Core
{
    internal abstract class TranChangeLogRecord: TranLogRecord
    {
        protected TranChangeLogRecord(ref ReadOnlySpan<byte> span) : base(ref span)
        {
            RowId = span.Read<DbRowId>();
            Offset = span.Read<ushort>();
        }

        public TranChangeLogRecord(DbTranId tranId, DbRowId rowId, ushort offset) : base(tranId)
        {
            RowId = rowId;
            Offset = offset;
        }

        public DbRowId RowId { get; }

        public ushort Offset { get; }

    }
}