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
            this.PerfCounters.InitializeCounter.Increment();
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
            using (var transaction = this.YawnSite.CreateTransaction())
            {
                var result = SaveRecord(inputInstance, transaction);
                transaction.Commit();
                return result;
            }
        }

        public IStorageLocation SaveRecord(YawnSchema instanceToSave, ITransaction transaction)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to write to database '{this.YawnSite.DatabaseName}' which is closed");
            }

            var instance = Cloner.Clone<T>(instanceToSave as T);
            if (instance == null)
            {
                return null;
            }

            ItemsInMemmory[instance.Id] = instance;

            this.PerfCounters.IndexingCounter.Increment();
            foreach (var index in this.Indicies.Values)
            {
                index.SetIndex(instance, new MemStorageLocation() { Id = instance.Id });
            }

            this.PerfCounters.RecordWriteCounter.Increment();

            return new MemStorageLocation() { Id = instance.Id };
        }

        public bool DeleteRecord(YawnSchema instance)
        {
            using (var transaction = this.YawnSite.CreateTransaction())
            {
                var result = DeleteRecord(instance, transaction);
                transaction.Commit();
                return result;
            }
        }

        public bool DeleteRecord(YawnSchema instance, ITransaction transaction)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to read from database '{this.YawnSite.DatabaseName}' which is closed");
            }

            foreach (var index in this.Indicies.Values)
            {
                index.DeleteIndex(instance);
            }

            this.PerfCounters.RecordDeleteCounter.Increment();
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
            return this.ItemsInMemmory.Values.Select(x=> PropagateSite(x as TE) as TE);
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
                this.PerfCounters.IndexingCounter.Increment();
                foreach (var index in needReindexing)
                {
                    index.SetIndex(record, new MemStorageLocation() { Id = record.Id });
                }
            }
        }

        public void Close()
        {

        }

        public void Open()
        {

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
            return true;
        }

        public bool RollbackTransactionItem(ITransactionItem transactionItem)
        {
            return true;
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
