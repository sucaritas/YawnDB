namespace YawnDB.Transactions
{
    using System;
    using System.Collections.Generic;
    using YawnDB.Interfaces;
    using System.Reflection;

    partial class Transaction : YawnSchema, ITransaction
    {
        public IYawn YawnSite { get; set; }

        public bool Commit()
        {
            // First record the intent to commit the transaction
            this.State = TransactionState.CommitStarted;
            this.YawnSite.SaveRecord(this);
            bool commitedOk = true;
            foreach (var item in this.TransactionItems)
            {
                if (item.Storage == null)
                {
                    Type schemaType = Type.GetType(item.SchemaType);
                    IStorage storage;
                    if (YawnSite.RegisteredStorageTypes.TryGetValue(schemaType, out storage))
                    {
                        item.Storage = storage;
                    }
                }

                commitedOk = item.Commit();
                if (!commitedOk)
                {
                    break;
                }
            }

            if(!commitedOk)
            {
                this.State = TransactionState.RollBackStarted;
                this.YawnSite.SaveRecord(this);
                foreach (var item in this.TransactionItems)
                {
                    item.Rollback();
                }

                this.State = TransactionState.RolledBack;
                this.YawnSite.SaveRecord(this);
                return false;
            }

            this.State = TransactionState.Commited;
            this.YawnSite.SaveRecord(this);
            return true;
        }

        public bool Rollback()
        {
            this.State = TransactionState.RollBackStarted;
            this.YawnSite.SaveRecord(this);

            foreach (var item in this.TransactionItems)
            {
                item.Rollback();
            }

            this.State = TransactionState.RolledBack;
            this.YawnSite.SaveRecord(this);
            return true;
        }

        public IStorageLocation SaveRecord(YawnSchema instanceToSave)
        {
            return default(IStorageLocation);
        }

        public bool DeleteRecord(YawnSchema instance)
        {
            return false;
        }

        public void AddTransactionItem(ITransactionItem transactionItem)
        {
            this.TransactionItems.AddLast(transactionItem as TransactionItem);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if(this.State == TransactionState.Created)
                    {
                        this.Rollback();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
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
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
