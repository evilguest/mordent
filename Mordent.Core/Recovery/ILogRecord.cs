using System;

namespace Mordent.Core
{
    internal interface ILogRecord
    {
        public LogRecordType RecordType { get; } // will be static in C# 10
        //public DbTranId TranId { get; }
        public DateTimeOffset Timestamp { get; }
        public Lsn WriteToLog(ILogFile logFile);
        public void Undo(IBuffers buffers);
    }
}