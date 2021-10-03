using System;
using System.Collections.Generic;
using System.Linq;

namespace Mordent.Core
{

    internal class RecoveryManager
    {

        public RecoveryManager(ILogFile logFile, IBuffers buffers, DbTranId tranId)
        {
            _logFile = logFile ?? throw new ArgumentNullException(nameof(logFile));
            _buffers = buffers ?? throw new ArgumentNullException(nameof(buffers));
            TranId = tranId;
            new TranStartRecord(TranId).WriteToLog(_logFile);
        }

        private ILogFile _logFile;
        private IBuffers _buffers; 
        public DbTranId TranId { get; }

        public void Commit()
        {
            _buffers.FlushAll(TranId);
            var lsn = new TranCommitRecord(TranId).WriteToLog(_logFile);
            _logFile.Flush(lsn);
        }

        public void Rollback()
        {
            DoRollback();
            _buffers.FlushAll(TranId);
            var lsn = new TranRollbackRecord(TranId).WriteToLog(_logFile);
            _logFile.Flush(lsn);
        }

        private void DoRollback()
        {
            foreach (var record in _logFile.Records.Select(b => LogRecord.Read(b.AsSpan())))
                if (record.TranId == TranId)
                    record.Undo(_buffers);
        }

        public void Recover()
        {
            DoRecover();
            _buffers.FlushAll(TranId);
            var lsn = new CheckPointRecord().WriteToLog(_logFile); // TODO: figure out the deal with the nonquiscient checkpoints!
            _logFile.Flush(lsn);

        }

        private void DoRecover()
        {
            var finishedTrans = new HashSet<DbTranId>();
            foreach(var record in _logFile.Records.Select(b=>LogRecord.Read(b.AsSpan())))
            {
                if (record.RecordType == LogRecordType.CheckPoint)
                    return; // recovery is complete
                if (record.RecordType == LogRecordType.TranCommit || record.RecordType == LogRecordType.TranRollback)
                    finishedTrans.Add(record.TranId);
                else if (!finishedTrans.Contains(record.TranId))
                    record.Undo(_buffers);
            }
        }

        internal Lsn RecordUpdate<T>(DbRowId rowId, ushort offset, T oldValue, T newValue) where T : unmanaged => new RowChangeRecord<T>(TranId, rowId, offset, oldValue, newValue).WriteToLog(_logFile);
        internal Lsn RecordUpdate(DbRowId rowId, ushort offset, string oldValue, string newValue) => new RowChangeRecord(TranId, rowId, offset, oldValue, newValue).WriteToLog(_logFile);
    }
}