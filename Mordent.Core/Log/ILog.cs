using System;
using System.Collections.Generic;
#if DEBUG
#endif

namespace Mordent.Core
{
    public interface ILogFile: IDisposable //, IAsyncDisposable
    {
        public Lsn Append(ReadOnlySpan<byte> data);
        public void Flush(Lsn upToLsn);
        public IEnumerable<byte[]> Records { get; }
    }

    public interface ILog
    {
        public void StartTransaction(DbTranId tranId);
        public void CommitTransaction(DbTranId tranId);
        public void RecordChange(DbTranId tranId, object oldData, object newData);
        public void StartCheckPoint();
        public void EndCheckPoint();

    }
}
