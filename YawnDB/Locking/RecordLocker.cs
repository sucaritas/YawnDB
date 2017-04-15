// <copyright file="RecordLocker.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Locking
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class RecordLocker : IRecordLocker
    {
        private ConcurrentDictionary<string, IRecordLockPair> locks = new ConcurrentDictionary<string, IRecordLockPair>();

        public IRecordUnlocker LockRecord(string id, RecordLockType lockType)
        {
            IRecordLockPair mylock = new RecordLockPair() { Id = id };
            mylock = this.locks.GetOrAdd(id, mylock);

            if ((lockType & RecordLockType.Read) == RecordLockType.Read)
            {
                Monitor.Enter(mylock.Reader);
            }

            if ((lockType & RecordLockType.Write) == RecordLockType.Write)
            {
                Monitor.Enter(mylock.Writer);
            }

            return new RecordUnlocker(this, mylock, lockType);
        }

        public void UnLockRecord(string id, RecordLockType lockType)
        {
            IRecordLockPair mylock;

            if (this.locks.TryGetValue(id, out mylock))
            {
                this.UnLockRecord(mylock, lockType);
            }
        }

        public void UnLockRecord(IRecordLockPair mylock, RecordLockType lockType)
        {
            if ((lockType & RecordLockType.Read) == RecordLockType.Read)
            {
                Monitor.Exit(mylock.Reader);
            }

            if ((lockType & RecordLockType.Write) == RecordLockType.Write)
            {
                Monitor.Exit(mylock.Writer);
            }
        }

        public bool WaitForRecordLock(string id, RecordLockType lockType)
        {
            IRecordLockPair mylock;
            bool hadToWait = false;

            if (this.locks.TryGetValue(id, out mylock))
            {
                if ((lockType & RecordLockType.Read) == RecordLockType.Read)
                {
                    if (Monitor.TryEnter(mylock.Reader))
                    {
                        Monitor.Exit(mylock.Reader);
                    }
                    else
                    {
                        Monitor.Enter(mylock.Reader);
                        Monitor.Exit(mylock.Reader);
                        hadToWait = true;
                    }
                }

                if ((lockType & RecordLockType.Write) == RecordLockType.Write)
                {
                    if (Monitor.TryEnter(mylock.Writer))
                    {
                        Monitor.Exit(mylock.Writer);
                    }
                    else
                    {
                        Monitor.Enter(mylock.Writer);
                        Monitor.Exit(mylock.Writer);
                        hadToWait = true;
                    }
                }
            }

            return hadToWait;
        }
    }
}
