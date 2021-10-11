using System;
using System.Linq;
using System.Threading;

namespace Mordent.Core
{
    public class Buffers : IBuffers
    {
        private struct BufferHeader
        {
            public DbPageId PageId;
            public int PinCount;
            public DbTranId TranId;
            public Lsn Lsn;
        }

        private readonly BufferHeader[] _headers;
        private readonly DbPage[] _pages;
        private readonly ILogFile _logFile;
        private readonly IFilesManager _filesManager;
        private const int MAX_WAIT_ATTEMPTS = 2;
        private const int MAX_WAIT_MILLIS = 5000;
        private object _lock = new();
        private ManualResetEventSlim _gotMoreBuffers = new ManualResetEventSlim();

        public int Available { get; private set; }

        public Buffers(ILogFile logFile, IFilesManager filesManager, int capacity)
        {
            _logFile = logFile;
            _filesManager = filesManager;
            _headers = new BufferHeader[capacity];
            foreach(ref var h in _headers.AsSpan())
                h.PageId = DbPageId.NotInit;

            _pages = new DbPage[capacity];
            Available = capacity;
        }

        private void AssignToPage(int bufferNo, DbPageId pageId)
        {
            Flush(bufferNo);
            _headers[bufferNo].PageId = pageId;
            _filesManager.ReadPage(pageId, ref _pages[bufferNo]);
            _headers[bufferNo].PinCount = 0;
        }

        private void Flush(int bufferNo)
        {
            if (_headers[bufferNo].TranId != DbTranId.None)
            {
                _logFile.Flush(_headers[bufferNo].Lsn);
                _filesManager.WritePage(_headers[bufferNo].PageId, ref _pages[bufferNo]);
                _headers[bufferNo].TranId = DbTranId.None;
            }
        }

//        public DbTranId GetModifyingTranId(int bufferNo) => _headers[bufferNo].TranId;

        public ref DbPage GetPage(int bufferNo) => ref _pages[bufferNo];

        //public DbPageId GetPageId(int bufferNo) => _headers[bufferNo].PageId;

        private bool IsPinned(int bufferNo) => _headers[bufferNo].PinCount > 0;

//        public void Pin(int bufferNo) => _headers[bufferNo].PinCount++;

        public void SetModified(int bufferNo, DbTranId tranId, Lsn lsn)
        {
            _headers[bufferNo].TranId = tranId;
            if (lsn != Lsn.None)
                _headers[bufferNo].Lsn = lsn;
        }

        public void Unpin(int bufferNo)
        {
            if(--_headers[bufferNo].PinCount <= 0)
            {
                Available++;
                _gotMoreBuffers.Set();
            }
        }

        public void FlushAll(DbTranId tranId)
        {
            lock (_lock)
                for (var i = 0; i < _headers.Length; i++)
                    if (_headers[i].TranId == tranId)
                        Flush(i);
        }

        public int Pin(DbPageId pageId)
        {
            for(var attempt = 0; attempt < MAX_WAIT_ATTEMPTS; attempt++)
            {
                lock(_lock)
                    if (TryToPin(pageId, out int bufferNo))
                        return bufferNo;
                _gotMoreBuffers.Wait(MAX_WAIT_MILLIS);
            }
            throw new Exception("Couldn't load buffer");
        }

        private bool TryToPin(DbPageId pageId, out int bufferNo)
        {
            bufferNo = FindExistingBuffer(pageId);
            if (bufferNo == -1)
            {
                bufferNo = ChooseUnpinnedBuffer();
                if (bufferNo == -1)
                    return false;
                AssignToPage(bufferNo, pageId);
            }
            if(!IsPinned(bufferNo))
                Available--;
            _headers[bufferNo].PinCount++;
            return true;
        }

        private int ChooseUnpinnedBuffer()
        {
            if (Available == 0)
                return -1;
            for (var i = 0; i < _headers.Length; i++)
                if (!IsPinned(i))
                    return i;
            _gotMoreBuffers.Reset();
            return -1;
        }

        private int FindExistingBuffer(DbPageId pageId)
        {
            for (var i = 0; i < _headers.Length; i++)
                if (_headers[i].PageId == pageId)
                    return i;
            return -1;
        }

        public int PinNew()
        {
            for (var attempt = 0; attempt < MAX_WAIT_ATTEMPTS; attempt++)
            {
                lock (_lock)
                    if (TryToPinNew(out int bufferNo))
                        return bufferNo;
                _gotMoreBuffers.Wait(MAX_WAIT_MILLIS);
            }
            throw new Exception("Couldn't load buffer");
        }
        public bool TryToPinNew(out int bufferNo)
        {
            bufferNo = ChooseUnpinnedBuffer();
            if (bufferNo == -1)
                return false;
            AssignToNew(bufferNo);
            Available--;
            _headers[bufferNo].PinCount++;
            return true;
        }

        private void AssignToNew(int bufferNo)
        {
            Flush(bufferNo);
            _headers[bufferNo].PageId = _filesManager.AddPage();
            _headers[bufferNo].PinCount = 0;
        }

        public DbPageId GetPageId(int bufferNo) => _headers[bufferNo].PageId;
    }
}
