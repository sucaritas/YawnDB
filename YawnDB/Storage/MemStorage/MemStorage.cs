// <copyright file="MemStorage.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Storage.MemStorage
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;
    using Bond;
    using Bond.IO.Unsafe;
    using Bond.Protocols;
    using YawnDB.EventSources;
    using YawnDB.Exceptions;
    using YawnDB.Interfaces;
    using YawnDB.PerformanceCounters;
    using YawnDB.Transactions;
    using YawnDB.Utils;

    public class MemStorage<T> : IStorage where T : YawnSchema
    {
        private IYawn yawnSite;

        public StorageState State { get; private set; } = StorageState.Closed;

        private IDictionary<long, T> itemsInMemmory = new ConcurrentDictionary<long, T>();

        private Cloner<T> cloner = new Cloner<T>(typeof(T));

        private StorageCounters perfCounters;

        private string typeNameNormilized;

        public string FullStorageName { get; }

        public IDictionary<string, IIndex> Indicies { get; } = new ConcurrentDictionary<string, IIndex>();

        public Type SchemaType { get; } = typeof(T);

        private object autoIdLock = new object();

        private long nextIndex = 0;

        public MemStorage(IYawn yawnSite)
        {
            this.yawnSite = yawnSite;
            this.typeNameNormilized = this.SchemaType.Namespace + "." + this.SchemaType.Name;
            if (this.SchemaType.IsGenericType)
            {
                this.typeNameNormilized += "[";
                foreach (var genericArgument in this.SchemaType.GetGenericArguments())
                {
                    this.typeNameNormilized += genericArgument.Namespace + "." + genericArgument.Name;
                }

                this.typeNameNormilized += "]";
            }

            this.FullStorageName = yawnSite.DatabaseName + "_" + this.typeNameNormilized;

            var proterties = typeof(T).GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in proterties)
            {
                if (typeof(IReference).IsAssignableFrom(prop.PropertyType))
                {
                    this.referencingProperties.Add(prop);
                }
            }

            this.perfCounters = new StorageCounters(this.FullStorageName);
        }

        public IStorageLocation InsertRecord(YawnSchema instanceToInsert)
        {
            return this.SaveRecord(instanceToInsert);
        }

        public IStorageLocation InsertRecord(YawnSchema instanceToInsert, ITransaction transaction)
        {
            return this.SaveRecord(instanceToInsert, transaction);
        }

        public IStorageLocation SaveRecord(YawnSchema inputInstance)
        {
            return this.SaveRecord(inputInstance, null);
        }

        public IStorageLocation SaveRecord(YawnSchema instanceToSave, ITransaction transaction)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to write to database '{this.yawnSite.DatabaseName}' which is closed");
            }

            T instance = this.cloner.Clone<T>(instanceToSave as T);
            T existingInstance;
            this.itemsInMemmory.TryGetValue(instance.Id, out existingInstance);

            if (transaction != null)
            {
                var transactionItem = new TransactionItem();
                transactionItem.ItemAction = Transactions.TransactionAction.Update;
                transactionItem.SchemaType = this.SchemaType.AssemblyQualifiedName;
                transactionItem.OldInstance = existingInstance ?? (T)Activator.CreateInstance(this.SchemaType);
                transactionItem.NewInstance = instance ?? (T)Activator.CreateInstance(this.SchemaType);
                transactionItem.Storage = this;
                transaction.AddTransactionItem(transactionItem);

                var transactionLocation = this.yawnSite.SaveRecord(transaction as YawnSchema);
                if (transactionLocation == null)
                {
                    return null;
                }

                return new MemStorageLocation() { Id = instanceToSave.Id };
            }

            StorageEventSource.Log.RecordWriteStart(this.FullStorageName, instance.Id);
            this.perfCounters.RecordWriteStartCounter.Increment();
            if (instance == null)
            {
                return null;
            }

            this.itemsInMemmory[instance.Id] = instance;

            StorageEventSource.Log.IndexingStart(this.FullStorageName, instance.Id);
            this.perfCounters.IndexingStartCounter.Increment();
            foreach (var index in this.Indicies.Values)
            {
                index.UpdateIndex(existingInstance, instance, new MemStorageLocation() { Id = instance.Id });
            }

            StorageEventSource.Log.IndexingFinish(this.FullStorageName, instance.Id);
            this.perfCounters.IndexingFinishedCounter.Increment();

            StorageEventSource.Log.RecordWriteFinish(this.FullStorageName, instance.Id);
            this.perfCounters.RecordWriteFinishedCounter.Increment();

            return new MemStorageLocation() { Id = instance.Id };
        }

        public bool DeleteRecord(YawnSchema instance)
        {
            return this.DeleteRecord(instance, null);
        }

        public bool DeleteRecord(YawnSchema instance, ITransaction transaction)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to read from database '{this.yawnSite.DatabaseName}' which is closed");
            }

            StorageEventSource.Log.RecordDeleteStart(this.FullStorageName, instance.Id);
            this.perfCounters.RecordDeleteStartCounter.Increment();

            if (transaction != null)
            {
                var transactionItem = new TransactionItem();
                transactionItem.ItemAction = Transactions.TransactionAction.Delete;
                transactionItem.SchemaType = this.SchemaType.AssemblyQualifiedName;
                transactionItem.OldInstance = instance ?? (T)Activator.CreateInstance(this.SchemaType);
                transactionItem.NewInstance = instance ?? (T)Activator.CreateInstance(this.SchemaType);
                transactionItem.Storage = this;
                transaction.AddTransactionItem(transactionItem);
                return this.yawnSite.SaveRecord(transaction as YawnSchema) == null ? false : true;
            }

            foreach (var index in this.Indicies.Values)
            {
                index.DeleteIndex(instance);
            }

            StorageEventSource.Log.RecordDeleteFinish(this.FullStorageName, instance.Id);
            this.perfCounters.RecordDeleteFinishedCounter.Increment();
            return this.itemsInMemmory.Remove(instance.Id);
        }

        private List<PropertyInfo> referencingProperties = new List<PropertyInfo>();

        public IEnumerable<TE> GetRecords<TE>(IEnumerable<IStorageLocation> recordsToPull) where TE : YawnSchema
        {
            List<T> records = new List<T>();
            foreach (var location in recordsToPull)
            {
                T record;
                if (this.itemsInMemmory.TryGetValue((location as MemStorageLocation).Id, out record))
                {
                    yield return record as TE;
                }
            }

            yield break;
        }

        public IEnumerable<TE> GetAllRecords<TE>() where TE : YawnSchema
        {
            return this.itemsInMemmory.Values.Select(x => this.PropagateSite(x as TE) as TE);
        }

        public YawnSchema CreateRecord()
        {
            var record = Activator.CreateInstance(typeof(T)) as T;
            record.Id = this.GetNextID();
            return this.PropagateSite(record as T) as T;
        }

        public long GetNextID()
        {
            lock (this.autoIdLock)
            {
                Interlocked.Increment(ref this.nextIndex);
            }

            return this.nextIndex;
        }

        public void ReIndexStorage(IList<IIndex> needReindexing)
        {
            if (!needReindexing.Any())
            {
                return;
            }

            foreach (var record in this.itemsInMemmory.Values)
            {
                this.perfCounters.IndexingStartCounter.Increment();
                foreach (var index in needReindexing)
                {
                    index.SetIndex(record, new MemStorageLocation() { Id = record.Id });
                }

                this.perfCounters.IndexingFinishedCounter.Increment();
            }
        }

        public void Close()
        {
        }

        public void Open()
        {
            this.perfCounters.InitializeCounter.Increment();
        }

        private YawnSchema PropagateSite(YawnSchema instance)
        {
            foreach (var prop in this.referencingProperties)
            {
                ((IReference)prop.GetValue(instance)).YawnSite = this.yawnSite;
            }

            return instance;
        }

        public IEnumerable<IStorageLocation> GetStorageLocations(IIdexArguments queryParams)
        {
            List<IStorageLocation> locations = new List<IStorageLocation>();
            foreach (var index in this.Indicies)
            {
                locations.AddRange(index.Value.GetStorageLocations(queryParams));
            }

            return locations;
        }

        public bool CommitTransactionItem(ITransactionItem transactionItem)
        {
            var item = transactionItem as TransactionItem;
            switch (item.ItemAction)
            {
                case Transactions.TransactionAction.Delete:
                    return this.DeleteRecord(item.OldInstance);

                case Transactions.TransactionAction.Update:
                case Transactions.TransactionAction.Insert:
                    return this.SaveRecord(item.NewInstance) == null ? false : true;

                default:
                    return false;
            }
        }

        public bool RollbackTransactionItem(ITransactionItem transactionItem)
        {
            var item = transactionItem as TransactionItem;
            switch (item.ItemAction)
            {
                case Transactions.TransactionAction.Update:
                case Transactions.TransactionAction.Insert:
                    return this.SaveRecord(item.OldInstance) == null ? false : true;

                case Transactions.TransactionAction.Delete:
                default:
                    return false;
            }
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
