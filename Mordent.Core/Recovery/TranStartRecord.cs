using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    internal class TranStartRecord : SimpleTranLogRecord
    {
        public override LogRecordType RecordType => LogRecordType.TranStart;

        public TranStartRecord(DbTranId tranId): base(tranId) {}
        public TranStartRecord(ref ReadOnlySpan<byte> span) : base(ref span) { }
    }
}