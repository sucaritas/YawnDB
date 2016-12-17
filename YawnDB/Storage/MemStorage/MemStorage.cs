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
    using Bond;
    using Bond.Protocols;
    using Bond.IO.Unsafe;
    using System.Runtime.Caching;
    using YawnDB.EventSources;
    using YawnDB.PerformanceCounters;
    using YawnDB.Interfaces;
    using YawnDB.Utils;

    public class MemStorage<T> : IStorageOf<T> where T : YawnSchema
    {
        private IDictionary<long, T> ItemsInMemmory = new ConcurrentDictionary<long, T>();

        private Cloner<T> Cloner = new Cloner<T>(typeof(T));

        private StorageCounters PerfCounters;

        private string TypeNameNormilized;

        public string FullStorageName { get; }

        public IDictionary<string, IIndex> Indicies { get; } = new ConcurrentDictionary<string, IIndex>();

        public Type SchemaType { get; } = typeof(T);

        private long NextIndex = 0;

        public MemStorage(IYawn yawnSite)
        {
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
            this.PerfCounters = new StorageCounters(this.FullStorageName);
            this.PerfCounters.InitializeCounter.Increment();
        }

        public async Task<IStorageLocation> SaveRecord(T instanceToSave)
        {
            var instance = Cloner.Clone<T>(instanceToSave);
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

        public bool DeleteRecord(T instance)
        {
            foreach (var index in this.Indicies.Values)
            {
                index.DeleteIndex(instance);
            }

            this.PerfCounters.RecordDeleteCounter.Increment();
            return ItemsInMemmory.Remove(instance.Id);
        }

        public async Task<IEnumerable<T>> GetRecords(IEnumerable<IStorageLocation> recordsToPull)
        {
            List<T> records = new List<T>();
            foreach (var location in recordsToPull)
            {
                T record;
                if (ItemsInMemmory.TryGetValue((location as MemStorageLocation).Id, out record))
                {
                    records.Add(record);
                }
            }

            return records.ToArray();
        }

        public async Task<IEnumerable<T>> GetAllRecords()
        {
            return this.ItemsInMemmory.Values;
        }

        public async Task<T> CreateRecord()
        {
            var record = Activator.CreateInstance(typeof(T)) as T;
            record.Id = this.GetNextID();
            return record as T;
        }

        public long GetNextID()
        {
            Interlocked.Increment(ref this.NextIndex);
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
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
