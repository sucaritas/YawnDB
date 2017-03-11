namespace YawnDB.Storage.MemStorage
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Threading;
    using System.Reflection;
    using Bond;
    using Bond.Protocols;
    using Bond.IO.Unsafe;
    using System.Runtime.Caching;
    using YawnDB.EventSources;
    using YawnDB.PerformanceCounters;
    using YawnDB.Interfaces;
    using YawnDB.Utils;
    using YawnDB.Exceptions;
    using YawnDB.Transactions;

    public class MemStorage<T> : IStorage where T : YawnSchema
    {
        private IYawn YawnSite;

        public StorageState State { get; private set; } = StorageState.Closed;

        private IDictionary<long, T> ItemsInMemmory = new ConcurrentDictionary<long, T>();

        private Cloner<T> Cloner = new Cloner<T>(typeof(T));

        private StorageCounters PerfCounters;

        private string TypeNameNormilized;

        public string FullStorageName { get; }

        public IDictionary<string, IIndex> Indicies { get; } = new ConcurrentDictionary<string, IIndex>();

        public Type SchemaType { get; } = typeof(T);

        private object AutoIdLock = new object();

        private long NextIndex = 0;

        public MemStorage(IYawn yawnSite)
        {
            this.YawnSite = yawnSite;
            this.TypeNameNormilized = this.SchemaType.Namespace + "." + this.SchemaType.Name;
            if (this.SchemaType.IsGenericType)
            {
                this.TypeNameNormilized += "[";
                foreach (var genericArgument in this.SchemaType.GetGenericArguments())
                {
                    this.TypeNameNormilized += genericArgument.Namespace + "." + genericArgument.Name;
                }
                this.TypeNameNormilized += "]";
            }

            this.FullStorageName = yawnSite.DatabaseName + "_" + this.TypeNameNormilized;

            var proterties = typeof(T).GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in proterties)
            {
                if (typeof(IReference).IsAssignableFrom(prop.PropertyType))
                {
                    ReferencingProperties.Add(prop);
                }
            }

            this.PerfCounters = new StorageCounters(this.FullStorageName);
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
            return SaveRecord(inputInstance, null);
        }

        public IStorageLocation SaveRecord(YawnSchema instanceToSave, ITransaction transaction)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to write to database '{this.YawnSite.DatabaseName}' which is closed");
            }

            T instance = Cloner.Clone<T>(instanceToSave as T);
            T existingInstance;
            ItemsInMemmory.TryGetValue(instance.Id, out existingInstance);

            if (transaction != null)
            {
                var transactionItem = new TransactionItem();
                transactionItem.ItemAction = Transactions.TransactionAction.Update;
                transactionItem.SchemaType = SchemaType.AssemblyQualifiedName;
                transactionItem.OldInstance = existingInstance ?? (T)Activator.CreateInstance(SchemaType);
                transactionItem.NewInstance = instance ?? (T)Activator.CreateInstance(SchemaType);
                transactionItem.Storage = this;
                transaction.AddTransactionItem(transactionItem);

                var transactionLocation = this.YawnSite.SaveRecord(transaction as YawnSchema);
                if (transactionLocation == null)
                {
                    return null;
                }

                return new MemStorageLocation() { Id = instanceToSave.Id };
            }

            StorageEventSource.Log.RecordWriteStart(this.FullStorageName, instance.Id);
            this.PerfCounters.RecordWriteStartCounter.Increment();
            if (instance == null)
            {
                return null;
            }

            ItemsInMemmory[instance.Id] = instance;

            StorageEventSource.Log.IndexingStart(this.FullStorageName, instance.Id);
            this.PerfCounters.IndexingStartCounter.Increment();
            foreach (var index in this.Indicies.Values)
            {
                index.UpdateIndex(existingInstance, instance, new MemStorageLocation() { Id = instance.Id });
            }

            StorageEventSource.Log.IndexingFinish(this.FullStorageName, instance.Id);
            this.PerfCounters.IndexingFinishedCounter.Increment();

            StorageEventSource.Log.RecordWriteFinish(this.FullStorageName, instance.Id);
            this.PerfCounters.RecordWriteFinishedCounter.Increment();

            return new MemStorageLocation() { Id = instance.Id };
        }

        public bool DeleteRecord(YawnSchema instance)
        {
            return DeleteRecord(instance, null);
        }

        public bool DeleteRecord(YawnSchema instance, ITransaction transaction)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to read from database '{this.YawnSite.DatabaseName}' which is closed");
            }

            StorageEventSource.Log.RecordDeleteStart(this.FullStorageName, instance.Id);
            this.PerfCounters.RecordDeleteStartCounter.Increment();

            if (transaction != null)
            {
                var transactionItem = new TransactionItem();
                transactionItem.ItemAction = Transactions.TransactionAction.Delete;
                transactionItem.SchemaType = SchemaType.AssemblyQualifiedName;
                transactionItem.OldInstance = instance ?? (T)Activator.CreateInstance(SchemaType);
                transactionItem.NewInstance = instance ?? (T)Activator.CreateInstance(SchemaType);
                transactionItem.Storage = this;
                transaction.AddTransactionItem(transactionItem);
                return this.YawnSite.SaveRecord(transaction as YawnSchema) == null ? false : true;
            }

            foreach (var index in this.Indicies.Values)
            {
                index.DeleteIndex(instance);
            }

            StorageEventSource.Log.RecordDeleteFinish(this.FullStorageName, instance.Id);
            this.PerfCounters.RecordDeleteFinishedCounter.Increment();
            return ItemsInMemmory.Remove(instance.Id);
        }

        private List<PropertyInfo> ReferencingProperties = new List<PropertyInfo>();

        public IEnumerable<TE> GetRecords<TE>(IEnumerable<IStorageLocation> recordsToPull) where TE : YawnSchema
        {
            List<T> records = new List<T>();
            foreach (var location in recordsToPull)
            {
                T record;
                if (ItemsInMemmory.TryGetValue((location as MemStorageLocation).Id, out record))
                {
                    yield return record as TE;
                }
            }

            yield break;
        }

        public IEnumerable<TE> GetAllRecords<TE>() where TE : YawnSchema
        {
            return this.ItemsInMemmory.Values.Select(x => PropagateSite(x as TE) as TE);
        }

        public YawnSchema CreateRecord()
        {
            var record = Activator.CreateInstance(typeof(T)) as T;
            record.Id = this.GetNextID();
            return PropagateSite(record as T) as T;
        }

        public long GetNextID()
        {
            lock (AutoIdLock)
            {
                Interlocked.Increment(ref this.NextIndex);
            }

            return this.NextIndex;
        }

        public void ReIndexStorage(IList<IIndex> needReindexing)
        {
            if (!needReindexing.Any())
            {
                return;
            }

            foreach (var record in this.ItemsInMemmory.Values)
            {
                this.PerfCounters.IndexingStartCounter.Increment();
                foreach (var index in needReindexing)
                {
                    index.SetIndex(record, new MemStorageLocation() { Id = record.Id });
                }

                this.PerfCounters.IndexingFinishedCounter.Increment();
            }
        }

        public void Close()
        {

        }

        public void Open()
        {
            this.PerfCounters.InitializeCounter.Increment();
        }

        private YawnSchema PropagateSite(YawnSchema instance)
        {
            foreach (var prop in ReferencingProperties)
            {
                ((IReference)prop.GetValue(instance)).YawnSite = this.YawnSite;
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
