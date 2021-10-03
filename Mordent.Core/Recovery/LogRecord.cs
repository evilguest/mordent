using System;
using System.Text;

namespace Mordent.Core
{
    internal abstract class LogRecord : ILogRecord
    {
        public static LogRecord Read(ReadOnlySpan<byte> span)
        {
            span.Read(out LogRecordType recordType);
            switch(recordType)
            {
                case LogRecordType.TranStart: return new TranStartRecord(ref span);
                case LogRecordType.TranCommit: return new TranCommitRecord(ref span);
                case LogRecordType.TranRollback: return new TranRollbackRecord(ref span);
                case LogRecordType.ChangeRowT: return CreateRowChangeRecord(ref span);
                case LogRecordType.ChangeRowString: return new RowChangeRecord(ref span);
                case LogRecordType.CheckPoint: return new CheckPointRecord(ref span);
                default: throw new InvalidOperationException($"Unknown record type: {recordType}");
            }
        }

        private static LogRecord CreateRowChangeRecord(ref ReadOnlySpan<byte> span)
        {
            var timestamp = span.Read<DateTimeOffset>();
            var tranId = span.Read<DbTranId>();
            var rowId = span.Read<DbRowId>();
            var offset = span.Read<ushort>();
            var typeBytes = span.ReadShort();
            var typeName = Encoding.UTF8.GetString(typeBytes);
            var type = Type.GetType(typeName);
            var logRecordType = typeof(RowChangeRecord<>).MakeGenericType(type);
            var l = (RowChangeRecordBase)Activator.CreateInstance(logRecordType, timestamp, tranId, rowId, offset);
            l.FinishRead(span);
            return l;
        }

        public DateTimeOffset Timestamp { get; init; }

        public LogRecord() => Timestamp = DateTimeOffset.UtcNow;
        protected LogRecord(ref ReadOnlySpan<byte> span) => Timestamp = span.Read<DateTimeOffset>();

        public abstract LogRecordType RecordType { get; }
        public DbTranId TranId { get; protected set; }

        public virtual void Undo(IBuffers buffers) { }

        public abstract Lsn WriteToLog(ILogFile logFile);
    }
}