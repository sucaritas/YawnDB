// <copyright file="RecordUnlocker.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Locking
{
    using System;

    public class RecordUnlocker : IRecordUnlocker, IDisposable
    {
        public IRecordLockPair SyncCounter { get; private set; } = null;

        public IRecordLocker RecordLocker { get; private set; } = null;

        public RecordLockType RecordLockType { get; private set; } = RecordLockType.Write;

        public RecordUnlocker(IRecordLocker recordLocker, IRecordLockPair syncCounter, RecordLockType recordLockType)
        {
            this.SyncCounter = syncCounter;
            this.RecordLocker = recordLocker;
            this.RecordLockType = recordLockType;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    this.RecordLocker.UnLockRecord(this.SyncCounter, this.RecordLockType);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                this.disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~RecordUnlocker() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);

            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
