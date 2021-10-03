using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.Runtime.InteropServices;
using System.Text;

namespace Mordent.Core
{
    #if DEBUG
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    #endif
    internal class RowChangeRecord<T> : RowChangeRecordBase
        where T : unmanaged
    {
        public RowChangeRecord(DbTranId tranId, DbRowId rowId, ushort offset, T oldValue, T newValue) : base(tranId, rowId, offset)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
        public RowChangeRecord(DateTimeOffset timeStamp, DbTranId tranId, DbRowId rowId, ushort offset) : base(tranId, rowId, offset) => Timestamp = timeStamp;
        public override void FinishRead(ReadOnlySpan<byte> span)
        {
            OldValue = span.Read<T>();
            NewValue = span.Read<T>();
        }
        public override Lsn WriteToLog(ILogFile logFile)
        {
            var typeBytes = TypeBytes;
            var l =
                sizeof(LogRecordType) +             // log record type
                Marshal.SizeOf<DateTimeOffset>() +  // timestamp
                Marshal.SizeOf<DbTranId>() +        // tran ID
                Marshal.SizeOf<DbRowId>() +         // row ID
                sizeof(short) +                     // offset len
                sizeof(short) +                     // type len length
                TypeBytes.Length +
                2 * Marshal.SizeOf<T>();
            var b = new byte[l];
            var w = b.AsSpan();
            w.Write(RecordType);
            w.Write(Timestamp);
            w.Write(TranId);
            w.Write(RowId);
            w.Write(Offset);
            w.WriteShort(typeBytes);
            w.Write(OldValue);
            w.Write(NewValue);

            return logFile.Append(b);
        }

        private static short TypeLen => (short)Encoding.UTF8.GetByteCount(TypeName);
        private static byte[] TypeBytes => Encoding.UTF8.GetBytes(TypeName);

        private static string TypeName => typeof(T).Name;

        public override LogRecordType RecordType => LogRecordType.ChangeRowT;

        public T OldValue { get; private set; }
        public T NewValue { get; private set; }

        public override string ToString() => $"Tx {TranId} [{Timestamp:s}] Changed row {RowId}@[{Offset} from {OldValue} to {NewValue}";
#if DEBUG
        private string DebuggerDisplay => ToString();
#endif
        public override void Undo(IBuffers buffers)
        {
            base.Undo(buffers);
            var bn = buffers.Pin(RowId.PageId);
            var s = buffers.GetPage(bn).RowData.GetSlotSpan(RowId.SlotNo);
            s.Write(OldValue);
            buffers.Unpin(bn);
        }

    }

    internal abstract class RowChangeRecordBase : TranChangeLogRecord
    {
        public RowChangeRecordBase(ref ReadOnlySpan<byte> span) : base(ref span){}

        public RowChangeRecordBase(DbTranId tranId, DbRowId rowId, ushort offset) : base(tranId, rowId, offset){}
        public abstract void FinishRead(ReadOnlySpan<byte> span);

    }

#if DEBUG
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
#endif
    internal class RowChangeRecord : TranChangeLogRecord
    {
        public RowChangeRecord(ref ReadOnlySpan<byte> span): base(ref span)
        {
            OldValue = Encoding.UTF8.GetString(span.ReadShort());
            NewValue = Encoding.UTF8.GetString(span.ReadShort());
        }

        public RowChangeRecord(DbTranId tranId, DbRowId rowId, ushort offset, string oldValue, string newValue) : base(tranId, rowId, offset)
        {
            OldValue = oldValue ?? throw new ArgumentNullException(nameof(oldValue));
            NewValue = newValue ?? throw new ArgumentNullException(nameof(newValue));
        }

        public string OldValue { get; private set; }
        public string NewValue { get; private set; }
        public override LogRecordType RecordType => LogRecordType.ChangeRowString;

        public override Lsn WriteToLog(ILogFile logFile)
        {
            var l =
                sizeof(LogRecordType) +             // log record type
                Marshal.SizeOf<DateTimeOffset>() +  // timestamp
                Marshal.SizeOf<DbTranId>() +        // tran ID
                Marshal.SizeOf<DbRowId>() +         // row ID
                sizeof(short) +                     // offset len
                sizeof(short) +                     // old value size length
                Encoding.UTF8.GetByteCount(OldValue) +
                sizeof(short) +                     // old value size length
                Encoding.UTF8.GetByteCount(NewValue);
            var b = new byte[l];
            var w = b.AsSpan();
            w.Write(RecordType);
            w.Write(Timestamp);
            w.Write(TranId);
            w.Write(RowId);
            w.Write(Offset);
            w.WriteShort(Encoding.UTF8.GetBytes(OldValue));
            w.WriteShort(Encoding.UTF8.GetBytes(NewValue));

            return logFile.Append(b);
        }

        public override void Undo(IBuffers buffers)
        {
            base.Undo(buffers);
            var bn = buffers.Pin(RowId.PageId);
            var s = buffers.GetPage(bn).RowData.GetSlotSpan(RowId.SlotNo);
            StringHelper.WriteString(buffers, s[Offset..], OldValue);
            buffers.Unpin(bn);
        }
    }
}