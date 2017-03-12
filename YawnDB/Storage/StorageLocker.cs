// <copyright file="StorageLocker.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class StorageLocker
    {
        private PerformanceCounter contentionCounter;

        private ConcurrentDictionary<long, StorageSyncLockCounter> locks = new ConcurrentDictionary<long, StorageSyncLockCounter>();

        public StorageLocker(PerformanceCounter contentionCounter)
        {
            this.contentionCounter = contentionCounter;
        }

        public StorageSyncLockCounter LockRecord(long id, object dependentLock)
        {
            StorageSyncLockCounter mylock = new StorageSyncLockCounter() { Id = id };
            mylock = this.locks.GetOrAdd(id, mylock);
            if (dependentLock != null)
            {
                lock (dependentLock)
                {
                    Interlocked.Increment(ref mylock.Readers);
                    return mylock;
                }
            }

            Interlocked.Increment(ref mylock.Readers);
            return mylock;
        }

        public void UnLockRecord(long id)
        {
            StorageSyncLockCounter mylock;

            if (this.locks.TryGetValue(id, out mylock))
            {
                Interlocked.Decrement(ref mylock.Readers);
            }
        }

        public void UnLockRecord(StorageSyncLockCounter mylock)
        {
            Interlocked.Decrement(ref mylock.Readers);
        }

        public void WaitForRecord(long id, int minReaders = 0)
        {
            StorageSyncLockCounter mylock;
            if (this.locks.TryGetValue(id, out mylock))
            {
                while (mylock.Readers > minReaders)
                {
                    this.contentionCounter.Increment();
                    Thread.Sleep(0);
                }
            }
        }

        public void WaitForAllReaders(int minReaders = 0)
        {
            foreach (var lockKV in this.locks)
            {
                if (lockKV.Value != null)
                {
                    while (lockKV.Value.Readers > minReaders)
                    {
                        Thread.Sleep(0);
                    }
                }
            }
        }
    }
}
