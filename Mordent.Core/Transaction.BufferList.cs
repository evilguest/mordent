using System;
using System.Collections.Generic;

namespace Mordent.Core
{
    public partial class Transaction
    {
        private class BufferList
        {
            private IDictionary<DbPageId, int> _bufferIds = new Dictionary<DbPageId, int>();
            private IList<DbPageId> _pinned = new List<DbPageId>();
            private IBuffers _buffers;
            public BufferList(IBuffers buffers) => _buffers = buffers ?? throw new ArgumentNullException(nameof(buffers));
            public int GetBufferNo(DbPageId pageId) => _bufferIds.TryGetValue(pageId, out var bufferNo) ? bufferNo : -1;

            public void Pin(DbPageId pageId)
            {
                _bufferIds[pageId] = _buffers.Pin(pageId);
                _pinned.Add(pageId);
            }

            //DbPageId PinNew()

            public void Unpin(DbPageId pageId)
            {
                _buffers.Unpin(_bufferIds[pageId]);
                _pinned.Remove(pageId);
                if (!_pinned.Contains(pageId))
                    _bufferIds.Remove(pageId);
            }

            public void UnpinAll()
            {
                foreach(var pageId in _pinned)
                    _buffers.Unpin(_bufferIds[pageId]);
                _bufferIds.Clear();
                _pinned.Clear();
            }
        }
    }
}
