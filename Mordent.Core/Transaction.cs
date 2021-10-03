using System;
using System.Runtime.InteropServices;

namespace Mordent.Core
{
    public partial class Transaction
    {
        private static DbTranId __nextTranId = new(0);
        private static object __lock = new();

        private readonly IFilesManager _filesManager;
        private readonly ILogFile _logFile;
        private readonly IBuffers _buffers;

        private readonly BufferList _myBuffers;

        private readonly RecoveryManager _recoveryManager;
        private readonly ConcurrencyManager _concurrencyManager = new();

        private DbTranId _tranId;

        public Transaction(IFilesManager filesManager, ILogFile logFile, IBuffers buffers)
        {
            _filesManager = filesManager ?? throw new System.ArgumentNullException(nameof(filesManager));
            _logFile = logFile ?? throw new System.ArgumentNullException(nameof(logFile));
            _buffers = buffers ?? throw new System.ArgumentNullException(nameof(buffers));
            _myBuffers = new BufferList(buffers);

            _tranId = NextTransactionId();
            _recoveryManager = new RecoveryManager(_logFile, _buffers, _tranId);
        }

        private static DbTranId NextTransactionId()
        {
            lock (__lock)
            {
                __nextTranId++;
                Console.WriteLine($"New transaction: {__nextTranId}");
                return __nextTranId;
            }
        }

        #region Lifecycle
        public void Commit()
        {
            _recoveryManager.Commit();
            _concurrencyManager.Release();
            _myBuffers.UnpinAll();
            Console.WriteLine($"Transaction {_tranId} committed");
        }

        public void Rollback()
        {
            _recoveryManager.Rollback();
            _concurrencyManager.Release();
            _myBuffers.UnpinAll();
            Console.WriteLine($"Transaction {_tranId} rolled back");
        }
        public void Recover()
        {
            _buffers.FlushAll(_tranId);
            _recoveryManager.Recover();
        }
        #endregion

        #region Buffers management
        public void Pin(DbPageId pageId) => _myBuffers.Pin(pageId);
        public void Unpin(DbPageId pageId) => _myBuffers.Unpin(pageId);
        public T Get<T>(DbRowId rowId, ushort offset) where T : unmanaged
        {
            _concurrencyManager.AcquireSharedLock(rowId);
            return MemoryMarshal.Read<T>(_buffers.GetPage(_myBuffers.GetBufferNo(rowId.PageId)).RowData.GetSlotSpan(rowId.SlotNo)[offset..]);
        }
        public void Set<T>(DbRowId rowId, ushort offset, ref T value) where T : unmanaged
        {
            _concurrencyManager.AcquireExclusiveLock(rowId);
            Lsn lsn = _recoveryManager.RecordUpdate(rowId, offset, Get<T>(rowId, offset), value);
            MemoryMarshal.Write(_buffers.GetPage(_myBuffers.GetBufferNo(rowId.PageId)).RowData.GetSlotSpan(rowId.SlotNo)[offset..], ref value);
        }
        public string Get(DbRowId rowId, ushort offset)
        {
            _concurrencyManager.AcquireSharedLock(rowId);
            return StringHelper.ReadString(_buffers, _buffers.GetPage(_myBuffers.GetBufferNo(rowId.PageId)).RowData.GetSlotSpan(rowId.SlotNo)[offset..]);
        }
        public void Set(DbRowId rowId, ushort offset, string value)
        {
            _concurrencyManager.AcquireExclusiveLock(rowId);
            Lsn lsn = _recoveryManager.RecordUpdate(rowId, offset, Get(rowId, offset), value);
            StringHelper.WriteString(_buffers, _buffers.GetPage(_myBuffers.GetBufferNo(rowId.PageId)).RowData.GetSlotSpan(rowId.SlotNo)[offset..], value);
        }
        public int AvailableBuffers { get; }

        #endregion
    }
}
