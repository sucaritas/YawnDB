// <copyright file="Yawn.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Bond;
    using YawnDB.Exceptions;
    using YawnDB.Index;
    using YawnDB.Locking;
    using YawnDB.Storage;
    using YawnDB.Storage.BlockStorage;
    using YawnDB.Transactions;

    public class Yawn : IYawn
    {
        public string DatabaseName { get; } = string.Empty;

        public string DefaultStoragePath { get; } = @".\";

        public Yawn(string databaseName, string defaultStoragePath)
        {
            this.DatabaseName = databaseName;
            this.DefaultStoragePath = defaultStoragePath;
        }

        public ConcurrentDictionary<Type, IReference> RegisteredTypes { get; } = new ConcurrentDictionary<Type, IReference>();

        public ConcurrentDictionary<Type, IStorage> RegisteredStorageTypes { get; } = new ConcurrentDictionary<Type, IStorage>();

        public bool TransactionsEnabled { get; private set; } = false;

        public IRecordLocker RecordLocker { get; private set; } = new RecordLocker();

        public bool RegisterSchema<T>() where T : YawnSchema
        {
            return this.RegisterSchema<T>(new BlockStorage<T>(this), new ReferenceTo<T>());
        }

        public bool RegisterSchema<T>(IStorage storage) where T : YawnSchema
        {
            IReferenceTo<T> referenceInstance = new ReferenceTo<T>();
            return this.RegisterSchema(storage, referenceInstance);
        }

        public bool RegisterSchema<T>(IStorage storage, IReferenceTo<T> referenceInstance) where T : YawnSchema
        {
            var schemaType = typeof(T);
            if (this.RegisteredTypes.TryAdd(schemaType, referenceInstance))
            {
                if (this.RegisteredStorageTypes.TryAdd(schemaType, storage))
                {
                    referenceInstance.YawnSite = this;
                    return true;
                }
                else
                {
                    IReference temp;
                    this.RegisteredTypes.TryRemove(schemaType, out temp);
                }
            }

            return false;
        }

        public bool UnRegisterSchema<T>()
        {
            return this.UnRegisterSchema(typeof(T));
        }

        public bool UnRegisterSchema(Type schemaToUnregister)
        {
            IReference schemaRef;
            if (this.RegisteredTypes.TryRemove(schemaToUnregister, out schemaRef))
            {
                schemaRef.YawnSite = null;
                return true;
            }

            return false;
        }

        public bool TryGetSchemaReference(Type schemaType, out IReference schemaRef)
        {
            return this.RegisteredTypes.TryGetValue(schemaType, out schemaRef);
        }

        public bool TryGetStorage(Type schemaType, out IStorage storage)
        {
            return this.RegisteredStorageTypes.TryGetValue(schemaType, out storage);
        }

        public T CreateRecord<T>() where T : YawnSchema
        {
            IStorage storage;
            this.RegisteredStorageTypes.TryGetValue(typeof(T), out storage);
            return storage.CreateRecord() as T;
        }

        public StorageLocation SaveRecord(YawnSchema instance, ITransaction transaction)
        {
            if (!this.TransactionsEnabled)
            {
                throw new DatabaseTransactionsAreDisabled();
            }

            IStorage storage;
            this.RegisteredStorageTypes.TryGetValue(instance.GetType(), out storage);
            return storage.SaveRecord(instance, transaction);
        }

        public StorageLocation SaveRecord(YawnSchema instance)
        {
            IStorage storage;
            this.RegisteredStorageTypes.TryGetValue(instance.GetType(), out storage);
            return storage.SaveRecord(instance);
        }

        public bool DeleteRecord(YawnSchema instance, ITransaction transaction)
        {
            if (!this.TransactionsEnabled)
            {
                throw new DatabaseTransactionsAreDisabled();
            }

            IStorage storage;
            this.RegisteredStorageTypes.TryGetValue(instance.GetType(), out storage);
            return storage.DeleteRecord(instance, transaction);
        }

        public bool DeleteRecord(YawnSchema instance)
        {
            IStorage storage;
            this.RegisteredStorageTypes.TryGetValue(instance.GetType(), out storage);
            return storage.DeleteRecord(instance);
        }

        public void Close()
        {
            foreach (var storage in this.RegisteredStorageTypes.Values)
            {
                if (storage != null)
                {
                    storage.Close();
                }
            }
        }

        public void Open(bool enableTransactions)
        {
            this.TransactionsEnabled = enableTransactions;

            if (this.TransactionsEnabled && !this.RegisteredStorageTypes.Keys.Contains(typeof(Transaction)))
            {
                this.RegisterSchema<Transaction>();
            }

            foreach (var storage in this.RegisteredStorageTypes.Values)
            {
                if (storage != null)
                {
                    storage.Open();
                }
            }

            if (this.TransactionsEnabled)
            {
                this.ReplayTransactionLog();
                this.PurgeTransactionLog();
            }
        }

        public ITransaction CreateTransaction()
        {
            IStorage storage;
            if (this.RegisteredStorageTypes.TryGetValue(typeof(Transaction), out storage))
            {
                var transaction = storage.CreateRecord() as Transaction;
                transaction.YawnSite = this;
                return transaction;
            }

            return null;
        }

        public void ReplayTransactionLog()
        {
            IStorage transactionStorage;
            this.RegisteredStorageTypes.TryGetValue(typeof(Transaction), out transactionStorage);
            foreach (var transaction in transactionStorage.GetAllRecords<Transaction>())
            {
                if (transaction.State == TransactionState.CommitStarted)
                {
                    transaction.YawnSite = this;
                    transaction.Commit();
                }

                transactionStorage.DeleteRecord(transaction);
            }
        }

        public void PurgeTransactionLog()
        {
            IStorage transactionStorage;
            this.RegisteredStorageTypes.TryGetValue(typeof(Transaction), out transactionStorage);
            foreach (var transaction in transactionStorage.GetAllRecords<Transaction>())
            {
                if (transaction.State != TransactionState.Created)
                {
                    transactionStorage.DeleteRecord(transaction);
                }
            }
        }

        public IRecordUnlocker LockRecord<T>(long id, RecordLockType lockType) where T : YawnSchema
        {
            return this.RecordLocker.LockRecord(this.GetLockName<T>(id), lockType);
        }

        public IRecordUnlocker LockRecord(long id, RecordLockType lockType, Type schemaType)
        {
            return this.RecordLocker.LockRecord(this.GetTypeName(schemaType) + "_" + id, lockType);
        }

        public IRecordUnlocker LockRecord(string id, RecordLockType lockType)
        {
            return this.RecordLocker.LockRecord(id, lockType);
        }

        public string GetLockName<T>(long id)
        {
            return this.GetLockName(id, typeof(T));
        }

        public string GetLockName(long id, Type type)
        {
            string name = this.GetTypeName(type);
            return name + "_" + id;
        }

        private string GetTypeName(Type type)
        {
            string name = type.FullName;
            if (type.IsGenericType)
            {
                name += "[";
                foreach (var arg in type.GetGenericArguments())
                {
                    name += this.GetTypeName(arg);
                }

                name += "]";
            }

            return name;
        }

        public T GetRecord<T>(long id) where T : YawnSchema
        {
            IStorage storage;
            if (this.RegisteredStorageTypes.TryGetValue(typeof(T), out storage))
            {
                IIndex keyIndex = storage.Indicies["YawnKeyIndex"];
                IBonded<StorageLocation> location = keyIndex.GetLocationForInstance(new YawnSchema() { Id = id });
                return storage.GetRecords<T>(new[] { location }).FirstOrDefault();
            }

            return default(T);
        }
    }
}
