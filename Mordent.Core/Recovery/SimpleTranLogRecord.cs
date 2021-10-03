using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    internal abstract class SimpleTranLogRecord : TranLogRecord
    {
        protected SimpleTranLogRecord(DbTranId tranId) : base(tranId){}

        protected SimpleTranLogRecord(ref ReadOnlySpan<byte> span): base(ref span){}


        public override Lsn WriteToLog(ILogFile logFile)
        {
            var t = (RecordType, Timestamp, TranId);
            return logFile.Append(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref t, 1)));
        }
    }
}