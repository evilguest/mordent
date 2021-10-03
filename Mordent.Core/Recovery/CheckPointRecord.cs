using System;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    internal class CheckPointRecord : LogRecord
    {
        public CheckPointRecord() { }
        public CheckPointRecord(ref ReadOnlySpan<byte> span) : base(ref span)
        {
        }

        public override LogRecordType RecordType => LogRecordType.CheckPoint;

        public override Lsn WriteToLog(ILogFile logFile)
        {
            var t = (RecordType, Timestamp);
            return logFile.Append(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref t, 1)));
        }
    }
}