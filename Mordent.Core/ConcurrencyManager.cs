using System;

namespace Mordent.Core
{
    internal class ConcurrencyManager
    {
        internal void Release()
        {
            throw new NotImplementedException();
        }

        internal void AcquireSharedLock(DbRowId rowId)
        {
            throw new NotImplementedException();
        }

        internal void AcquireExclusiveLock(DbRowId rowId)
        {
            throw new NotImplementedException();
        }
    }
}