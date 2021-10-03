using System;

namespace Mordent.Core
{
    internal abstract class TranLogRecord : LogRecord
    {
        public TranLogRecord(DbTranId tranId) => TranId = tranId;
        protected TranLogRecord(ref ReadOnlySpan<byte> span) : base(ref span) => TranId = span.Read<DbTranId>();

        protected TranLogRecord(){}
    }
}