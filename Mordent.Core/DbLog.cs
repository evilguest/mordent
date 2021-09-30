using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public static class LogHelper
    {
        internal static unsafe void Write<T>(this BinaryWriter writer, in T data)
            where T : unmanaged => writer.Write(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref Unsafe.AsRef(in data)), sizeof(T)));
        internal static void Write(this BinaryWriter writer, DbLog.LogRecordType logRecordType)
            => writer.Write((byte)logRecordType);
    }
    public class DbLog : ILog
    {
        private readonly object _syncRoot = new object();
        internal enum LogRecordType : byte
        {
            None,
            TranStart,
            TranCommit,
            TranRollback,
            CheckPointStart,
            CheckpointEnd,
            ChangeRow,
            ChangeKey
        };
        private BinaryWriter LogWriter { get; }
        private ISet<Guid> ActiveTransactions { get; } = new HashSet<Guid>();
        public DbLog(string fileName) : this(
            File.OpenWrite(
                string.IsNullOrWhiteSpace(fileName) 
                ? throw new ArgumentException($"'{nameof(fileName)}' cannot be null or whitespace.", nameof(fileName)) 
                : fileName)) { }
        public DbLog(Stream logStream) : this(new BinaryWriter(logStream ?? throw new ArgumentNullException(nameof(logStream)))) { }
        public DbLog(BinaryWriter writer) => LogWriter = writer ?? throw new ArgumentNullException(nameof(writer));

        public void StartTransaction(Guid transactionId)
        {
            lock (_syncRoot)
            {
                Contract.Requires(!ActiveTransactions.Contains(transactionId));
                Contract.Ensures(ActiveTransactions.Contains(transactionId));
                ActiveTransactions.Add(transactionId);
                LogWriter.Write(LogRecordType.TranStart);
                LogWriter.Write(transactionId);
                //LogWriter.Flush(); // no flush needed
            }
        }

        public void CommitTransaction(Guid transactionId)
        {
            lock (_syncRoot)
            {
                Contract.Requires(ActiveTransactions.Contains(transactionId));
                Contract.Ensures(!ActiveTransactions.Contains(transactionId));
                LogWriter.Write(LogRecordType.TranCommit);
                LogWriter.Write(transactionId);
                LogWriter.Flush(); // important! Commit requires flush.
                ActiveTransactions.Remove(transactionId);
            }
        }


        public void RecordChange(Guid transactionId, object oldData, object newData)
        {
            lock (_syncRoot)
            {
                throw new NotImplementedException();
            }
        }

        public void StartCheckPoint()
        {
            lock(_syncRoot)
            {
                LogWriter.Write(LogRecordType.CheckPointStart);
                LogWriter.Write(ActiveTransactions.Count);
                foreach (var t in ActiveTransactions)
                    LogWriter.Write(t);
                LogWriter.Flush();
            }
        }

        public void EndCheckPoint()
        {
            lock (_syncRoot)
            {
                LogWriter.Write(LogRecordType.CheckpointEnd);
                LogWriter.Flush();
            }
        }
    }
}
