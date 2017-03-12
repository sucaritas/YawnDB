// <copyright file="StorageUnlocker.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class StorageUnlocker : IDisposable
    {
        public StorageSyncLockCounter SyncCounter { get; private set; } = null;

        public StorageLocker RecordLocker { get; private set; } = null;

        public StorageUnlocker(StorageSyncLockCounter syncCounter, StorageLocker recordLocker)
        {
            this.RecordLocker = recordLocker;
            this.SyncCounter = syncCounter;
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
                    this.RecordLocker.UnLockRecord(this.SyncCounter);
                    this.RecordLocker = null;
                    this.SyncCounter = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                this.disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~StorageUnlocker() {
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
