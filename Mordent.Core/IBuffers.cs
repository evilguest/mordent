using System;

namespace Mordent.Core
{
    public interface IBuffers
    {
        public ref DbPage GetPage(int bufferNo);
        public void SetModified(int bufferNo, DbTranId tranId, Lsn lsn);
        public int Available { get; }
        public void FlushAll(DbTranId tranId);
        public void Unpin(int bufferNo);
        public int Pin(DbPageId pageId);
        public int PinNew();
        public DbPageId GetPageId(int bufferNo);
    }
}
