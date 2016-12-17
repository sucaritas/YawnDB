namespace YawnDB.Storage.BlockStorage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Diagnostics;

    public class BlockStorageSyncLock
    {
        public int Readers = 0;
    }

    public class BlockStorageLocker
    {
        private PerformanceCounter ContentionCounter;

        private ConcurrentDictionary<long, BlockStorageSyncLock> Locks = new ConcurrentDictionary<long, BlockStorageSyncLock>();

        public BlockStorageLocker(PerformanceCounter contentionCounter)
        {
            this.ContentionCounter = contentionCounter;
        }

        public BlockStorageSyncLock LockRecord(long id)
        {
            BlockStorageSyncLock mylock = new BlockStorageSyncLock(); ;

            mylock = Locks.GetOrAdd(id, mylock);
            Interlocked.Increment(ref mylock.Readers);
            return mylock;
        }

        public void UnLockRecord(long id)
        {
            BlockStorageSyncLock mylock;

            if (Locks.TryGetValue(id, out mylock))
            {
                Interlocked.Decrement(ref mylock.Readers);

                if (mylock.Readers == 0)
                {
                    lock (mylock)
                    {
                        if (mylock.Readers == 0)
                        {
                            BlockStorageSyncLock temp;
                            Locks.TryRemove(id, out temp);
                        }
                    }
                }
            }
        }

        public void WaitForRecord(long id, int minReaders = 0)
        {
            BlockStorageSyncLock mylock;
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
                        ContentionCounter.Increment();
                        Thread.Sleep(0);
                    }
                }
            }
        }
    }
}
