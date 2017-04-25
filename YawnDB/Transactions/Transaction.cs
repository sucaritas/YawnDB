// <copyright file="Transaction.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Transactions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using YawnDB.Locking;
    using YawnDB.Storage;

    public partial class Transaction : YawnSchema, ITransaction
    {
        public IYawn YawnSite { get; set; }

        private List<IRecordUnlocker> recordLocks;

        public bool Commit()
        {
            // First record the intent to commit the transaction
            this.State = TransactionState.CommitStarted;
            this.YawnSite.SaveRecord(this);

            // Lock all records in transaction for writing, locks are taken on an alpha ordered way
            var transactionItems = this.TransactionItems.Select(x => x.Deserialize()).ToArray();
            this.recordLocks = transactionItems.Select(ti => this.YawnSite.GetLockName(ti.NewInstance.Deserialize().Id, Type.GetType(ti.SchemaType)))
                                            .OrderBy(ln => ln)
                                            .ToArray()
                                            .Select(ln => this.YawnSite.LockRecord(ln, RecordLockType.Write))
                                            .ToList();

            bool commitedOk = true;
            foreach (var bondedItem in this.TransactionItems)
            {
                var item = bondedItem.Deserialize<TransactionItem>();
                this.SetStorageOnTransactionItem(ref item);

                commitedOk = item.Commit(bondedItem);
                if (!commitedOk)
                {
                    break;
                }
            }

            if (!commitedOk)
            {
                this.State = TransactionState.RollBackStarted;
                this.YawnSite.SaveRecord(this);
                foreach (var bondedItem in this.TransactionItems)
                {
                    var item = bondedItem.Deserialize<TransactionItem>();
                    item.Rollback(bondedItem);
                }

                this.State = TransactionState.RolledBack;
                this.YawnSite.SaveRecord(this);
                return false;
            }

            this.State = TransactionState.Commited;
            this.YawnSite.SaveRecord(this);

            // Release all locks;
            foreach (var unlocker in this.recordLocks)
            {
                unlocker.Dispose();
            }

            return true;
        }

        public bool Rollback()
        {
            this.State = TransactionState.RollBackStarted;
            this.YawnSite.SaveRecord(this);

            foreach (var bondedItem in this.TransactionItems)
            {
                var item = bondedItem.Deserialize<TransactionItem>();
                this.SetStorageOnTransactionItem(ref item);
                item.Rollback(bondedItem);
            }

            this.State = TransactionState.RolledBack;
            this.YawnSite.SaveRecord(this);
            return true;
        }

        public StorageLocation SaveRecord(YawnSchema instanceToSave)
        {
            return this.YawnSite.SaveRecord(instanceToSave, this);
        }

        public bool DeleteRecord(YawnSchema instance)
        {
            return this.YawnSite.DeleteRecord(instance, this);
        }

        public void AddTransactionItem<T>(T transactionItem) where T : TransactionItem
        {
            this.TransactionItems.AddLast(new Bond.Bonded<T>(transactionItem));
        }

        private void SetStorageOnTransactionItem(ref TransactionItem item)
        {
            if (item.Storage == null)
            {
                Type schemaType = Type.GetType(item.SchemaType);
                IStorage storage;
                if (this.YawnSite.RegisteredStorageTypes.TryGetValue(schemaType, out storage))
                {
                    item.Storage = storage;
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (this.State == TransactionState.Created)
                    {
                        this.Rollback();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                this.disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Transaction() {
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
