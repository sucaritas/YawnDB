﻿// <copyright file="StorageCounters.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.PerformanceCounters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class StorageCounters
    {
        public const string CounterCategoryName = "YawnDB Storage Counters";

        public const string InitializeCounterName = "# Times Storage has initialized";

        public const string RecordWriteStartCounterName = "# Record Write Start";
        public const string RecordWriteFinishedCounterName = "# Record Writes Finished";

        public const string RecordReadStartCounterName = "# Record Read Start";
        public const string RecordReadFinishedCounterName = "# Record Read Finished";

        public const string RecordReadFromCacheCounterName = "# Record Reads From Cache";

        public const string RecordDeleteStartCounterName = "# Record Delete Start";
        public const string RecordDeleteFinishedCounterName = "# Record Delete Finished";

        public const string IndexingStartCounterName = "# Record Indexing Start";
        public const string IndexingFinishedCounterName = "# Record Indexing Finished";

        public const string ResizeCounterName = "# Storage resizes";
        public const string WriteContentionCounterName = "# Write contentions";

        public PerformanceCounter InitializeCounter { get; private set; }

        public PerformanceCounter RecordWriteStartCounter { get; private set; }

        public PerformanceCounter RecordWriteFinishedCounter { get; private set; }

        public PerformanceCounter RecordReadStartCounter { get; private set; }

        public PerformanceCounter RecordReadFinishedCounter { get; private set; }

        public PerformanceCounter RecordReadFromCacheCounter { get; private set; }

        public PerformanceCounter RecordDeleteStartCounter { get; private set; }

        public PerformanceCounter RecordDeleteFinishedCounter { get; private set; }

        public PerformanceCounter IndexingStartCounter { get; private set; }

        public PerformanceCounter IndexingFinishedCounter { get; private set; }

        public PerformanceCounter ResizeCounter { get; private set; }

        public PerformanceCounter WriteContentionCounter { get; private set; }

        public StorageCounters(string instanceName)
        {
            if (SetupCounters())
            {
                while (!PerformanceCounterCategory.Exists(CounterCategoryName))
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }

            this.InitializeCounter = new PerformanceCounter(CounterCategoryName, InitializeCounterName, instanceName, false);
            this.InitializeCounter.RawValue = 0;

            this.RecordWriteStartCounter = new PerformanceCounter(CounterCategoryName, RecordWriteStartCounterName, instanceName, false);
            this.RecordWriteStartCounter.RawValue = 0;
            this.RecordWriteFinishedCounter = new PerformanceCounter(CounterCategoryName, RecordWriteFinishedCounterName, instanceName, false);
            this.RecordWriteFinishedCounter.RawValue = 0;

            this.RecordReadStartCounter = new PerformanceCounter(CounterCategoryName, RecordReadStartCounterName, instanceName, false);
            this.RecordReadStartCounter.RawValue = 0;
            this.RecordReadFinishedCounter = new PerformanceCounter(CounterCategoryName, RecordReadFinishedCounterName, instanceName, false);
            this.RecordReadFinishedCounter.RawValue = 0;

            this.RecordReadFromCacheCounter = new PerformanceCounter(CounterCategoryName, RecordReadFromCacheCounterName, instanceName, false);
            this.RecordReadFromCacheCounter.RawValue = 0;

            this.RecordDeleteStartCounter = new PerformanceCounter(CounterCategoryName, RecordDeleteStartCounterName, instanceName, false);
            this.RecordDeleteStartCounter.RawValue = 0;
            this.RecordDeleteFinishedCounter = new PerformanceCounter(CounterCategoryName, RecordDeleteFinishedCounterName, instanceName, false);
            this.RecordDeleteFinishedCounter.RawValue = 0;

            this.IndexingStartCounter = new PerformanceCounter(CounterCategoryName, IndexingStartCounterName, instanceName, false);
            this.IndexingStartCounter.RawValue = 0;
            this.IndexingFinishedCounter = new PerformanceCounter(CounterCategoryName, IndexingFinishedCounterName, instanceName, false);
            this.IndexingFinishedCounter.RawValue = 0;

            this.ResizeCounter = new PerformanceCounter(CounterCategoryName, ResizeCounterName, instanceName, false);
            this.ResizeCounter.RawValue = 0;

            this.WriteContentionCounter = new PerformanceCounter(CounterCategoryName, WriteContentionCounterName, instanceName, false);
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

                CounterCreationData recordWriteStartCount = new CounterCreationData();
                recordWriteStartCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordWriteStartCount.CounterName = RecordWriteStartCounterName;
                counterDataCollection.Add(recordWriteStartCount);

                CounterCreationData recordWriteFinishedCount = new CounterCreationData();
                recordWriteFinishedCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordWriteFinishedCount.CounterName = RecordWriteFinishedCounterName;
                counterDataCollection.Add(recordWriteFinishedCount);

                CounterCreationData recordReadStartCount = new CounterCreationData();
                recordReadStartCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordReadStartCount.CounterName = RecordReadStartCounterName;
                counterDataCollection.Add(recordReadStartCount);

                CounterCreationData recordReadFinishedCount = new CounterCreationData();
                recordReadFinishedCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordReadFinishedCount.CounterName = RecordReadFinishedCounterName;
                counterDataCollection.Add(recordReadFinishedCount);

                CounterCreationData recordReadCacheCount = new CounterCreationData();
                recordReadCacheCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordReadCacheCount.CounterName = RecordReadFromCacheCounterName;
                counterDataCollection.Add(recordReadCacheCount);

                CounterCreationData recordDeleteStartCount = new CounterCreationData();
                recordDeleteStartCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordDeleteStartCount.CounterName = RecordDeleteStartCounterName;
                counterDataCollection.Add(recordDeleteStartCount);

                CounterCreationData recordDeleteFinishedCount = new CounterCreationData();
                recordDeleteFinishedCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                recordDeleteFinishedCount.CounterName = RecordDeleteFinishedCounterName;
                counterDataCollection.Add(recordDeleteFinishedCount);

                CounterCreationData indexingStartCount = new CounterCreationData();
                indexingStartCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                indexingStartCount.CounterName = IndexingStartCounterName;
                counterDataCollection.Add(indexingStartCount);

                CounterCreationData indexingFinishedCount = new CounterCreationData();
                indexingFinishedCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                indexingFinishedCount.CounterName = IndexingFinishedCounterName;
                counterDataCollection.Add(indexingFinishedCount);

                CounterCreationData resizeCount = new CounterCreationData();
                resizeCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                resizeCount.CounterName = ResizeCounterName;
                counterDataCollection.Add(resizeCount);

                CounterCreationData writeContentionCount = new CounterCreationData();
                writeContentionCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                writeContentionCount.CounterName = WriteContentionCounterName;
                counterDataCollection.Add(writeContentionCount);

                PerformanceCounterCategory.Create(CounterCategoryName, "Perfomance counters for all YawnDB storages.", PerformanceCounterCategoryType.MultiInstance, counterDataCollection);

                return true;
            }

            return false;
        }
    }
}
