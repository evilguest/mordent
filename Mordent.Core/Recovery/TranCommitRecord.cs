using System;

namespace Mordent.Core
{
    internal class TranCommitRecord: SimpleTranLogRecord
    {
        public TranCommitRecord(DbTranId tranId): base(tranId){}

        public TranCommitRecord(ref ReadOnlySpan<byte> span): base(ref span){}

        public override LogRecordType RecordType => LogRecordType.TranCommit;
    }
}