namespace YawnDB.PerformanceCounters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    public class StorageCounters
    {
        public int Scale = 1000000;
        public const string CounterCategoryName = "YawnDB Storage Counters";

        public const string InitializeCounterName = "# Times Storage has initialized";
        public const string RecordWriteCounterName = "# Record Writes";
        public const string RecordReadCounterName = "# Record Reads";
        public const string RecordReadFromCacheCounterName = "# Record Reads From Cache";
        public const string RecordDeleteCounterName = "# Record deletes";
        public const string IndexingCounterName = "# Record indexing";
        public const string ResizeCounterName = "# Storage resizes";
        public const string WriteContentionCounterName = "# Write contentions";

        public PerformanceCounter InitializeCounter { get; private set;}
        public PerformanceCounter RecordWriteCounter { get; private set; }
        public PerformanceCounter RecordReadCounter { get; private set; }
        public PerformanceCounter RecordReadFromCacheCounter { get; private set; }
        public PerformanceCounter RecordDeleteCounter { get; private set; }
        public PerformanceCounter IndexingCounter { get; private set; }
        public PerformanceCounter ResizeCounter { get; private set; }

        public PerformanceCounter WriteContentionCounter { get; private set; }

        public StorageCounters(string InstanceName)
        {
            if(SetupCounters())
            {
                while (!PerformanceCounterCategory.Exists(CounterCategoryName))
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }

            this.InitializeCounter = new PerformanceCounter(CounterCategoryName, InitializeCounterName, InstanceName, false);
            this.InitializeCounter.RawValue = 0;

            this.RecordWriteCounter = new PerformanceCounter(CounterCategoryName, RecordWriteCounterName, InstanceName, false);
            this.RecordWriteCounter.RawValue = 0;

            this.RecordReadCounter = new PerformanceCounter(CounterCategoryName, RecordReadCounterName, InstanceName, false);
            this.RecordReadCounter.RawValue = 0;

            this.RecordReadFromCacheCounter = new PerformanceCounter(CounterCategoryName, RecordReadFromCacheCounterName, InstanceName, false);
            this.RecordReadFromCacheCounter.RawValue = 0;

            this.RecordDeleteCounter = new PerformanceCounter(CounterCategoryName, RecordDeleteCounterName, InstanceName, false);
            this.RecordDeleteCounter.RawValue = 0;

            this.IndexingCounter = new PerformanceCounter(CounterCategoryName, IndexingCounterName, InstanceName, false);
            this.IndexingCounter.RawValue = 0; 

            this.ResizeCounter = new PerformanceCounter(CounterCategoryName, ResizeCounterName, InstanceName, false);
            this.ResizeCounter.RawValue = 0;

            this.WriteContentionCounter = new PerformanceCounter(CounterCategoryName, WriteContentionCounterName, InstanceName, false);
            this.WriteContentionCounter.RawValue = 0; 
        }

        public static bool SetupCounters()
        {
            if (!PerformanceCounterCategory.Exists(CounterCategoryName))
            {
                CounterCreationDataCollection counterDataCollection = new CounterCreationDataCollection();

                CounterCreationData initializeCount = new CounterCreationData();
                initializeCount.CounterType = PerformanceCounterType.NumberOfItems32;
                initializeCount.CounterName = InitializeCounterName;
                counterDataCollection.Add(initializeCount);

                CounterCreationData recordWriteCount = new CounterCreationData();
                recordWriteCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordWriteCount.CounterName = RecordWriteCounterName;
                counterDataCollection.Add(recordWriteCount);

                CounterCreationData recordReadCount = new CounterCreationData();
                recordReadCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordReadCount.CounterName = RecordReadCounterName;
                counterDataCollection.Add(recordReadCount);

                CounterCreationData recordReadCacheCount = new CounterCreationData();
                recordReadCacheCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordReadCacheCount.CounterName = RecordReadFromCacheCounterName;
                counterDataCollection.Add(recordReadCacheCount);

                CounterCreationData recordDeleteCount = new CounterCreationData();
                recordDeleteCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordDeleteCount.CounterName = RecordDeleteCounterName;
                counterDataCollection.Add(recordDeleteCount);

                CounterCreationData IndexingCount = new CounterCreationData();
                IndexingCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                IndexingCount.CounterName = IndexingCounterName;
                counterDataCollection.Add(IndexingCount);

                CounterCreationData ResizeCount = new CounterCreationData();
                ResizeCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                ResizeCount.CounterName = ResizeCounterName;
                counterDataCollection.Add(ResizeCount);

                CounterCreationData WriteContentionCount = new CounterCreationData();
                WriteContentionCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                WriteContentionCount.CounterName = WriteContentionCounterName;
                counterDataCollection.Add(WriteContentionCount);

                PerformanceCounterCategory.Create(CounterCategoryName,
                "Perfomance counters for all YawnDB storages.",
                PerformanceCounterCategoryType.MultiInstance, counterDataCollection);

                return true;
            }

            return false;
        }
    }
}
