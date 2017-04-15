// <copyright file="IRecordLocker.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Locking
{
    public interface IRecordLocker
    {
        IRecordUnlocker LockRecord(string id, RecordLockType lockType);

        void UnLockRecord(string id, RecordLockType lockType);

        void UnLockRecord(IRecordLockPair mylock, RecordLockType lockType);

        bool WaitForRecordLock(string id, RecordLockType lockType);
    }
}
