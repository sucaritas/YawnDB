namespace YawnDB.Storage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Diagnostics;

    public class StorageSyncLockCounter
    {
        public long Id;
        public int Readers = 0;
    }

    public class StorageLocker
    {
        private PerformanceCounter ContentionCounter;

        private ConcurrentDictionary<long, StorageSyncLockCounter> Locks = new ConcurrentDictionary<long, StorageSyncLockCounter>();

        public StorageLocker(PerformanceCounter contentionCounter)
        {
            this.ContentionCounter = contentionCounter;
        }

        public StorageSyncLockCounter LockRecord(long id, object dependentLock)
        {
            StorageSyncLockCounter mylock = new StorageSyncLockCounter() { Id = id } ;
            mylock = Locks.GetOrAdd(id, mylock);
            if (dependentLock != null)
            {
                lock (dependentLock)
                {
                    Interlocked.Increment(ref mylock.Readers);
                }
            }

            Interlocked.Increment(ref mylock.Readers);
            return mylock;
        }

        public void UnLockRecord(long id)
        {
            StorageSyncLockCounter mylock;

            if (Locks.TryGetValue(id, out mylock))
            {
                Interlocked.Decrement(ref mylock.Readers);
            }
        }

        public void WaitForRecord(long id, int minReaders = 0)
        {
            StorageSyncLockCounter mylock;
            if (Locks.TryGetValue(id, out mylock))
            {
                while (mylock.Readers > minReaders)
                {
                    ContentionCounter.Increment();
                    Thread.Sleep(0);
                }
            }
        }

        public void WaitForAllReaders(int minReaders = 0)
        {
            foreach (var lockKV in Locks)
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
