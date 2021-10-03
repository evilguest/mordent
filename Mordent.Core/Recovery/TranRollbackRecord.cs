using System;

namespace Mordent.Core
{
    internal class TranRollbackRecord : SimpleTranLogRecord
    {
        public TranRollbackRecord(DbTranId tranId) : base(tranId) {}
        public TranRollbackRecord(ref ReadOnlySpan<byte> span) : base(ref span) { }

        public override LogRecordType RecordType => LogRecordType.TranRollback;
    }
}