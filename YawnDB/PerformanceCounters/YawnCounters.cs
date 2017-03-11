namespace YawnDB.PerformanceCounters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    public class YawnCounters
    {
        public const string CounterCategoryName = "YawnDB Counters";

        public const string InitializeCounterName = "# YawnDB Has Initialized";

        public const string TransactionCreatedCounterName = "# Transactions Created";
        public const string TransactionCommitStartCounterName = "# Transaction Commit Start";
        public const string TransactionCommitFailedCounterName = "# Transaction Commit Fail";
        public const string TransactionCommitFinishedCounterName = "# Transaction Commit Finished";
        public const string TransactionRollbackStartCounterName = "# Transaction Rollback Start";
        public const string TransactionRollbackFailedCounterName = "# Transaction Rollback Failed";
        public const string TransactionRollbackFinishedCounterName = "# Transaction Rollback Finished";


        public PerformanceCounter InitializeCounter { get; private set; }
        public PerformanceCounter TransactionCommitStartCounter { get; private set; }
        public PerformanceCounter TransactionCommitFailedCounter { get; private set; }
        public PerformanceCounter TransactionCommitFinishedCounter { get; private set; }
        public PerformanceCounter TransactionRollbackStartCounter { get; private set; }
        public PerformanceCounter TransactionRollbackFailedCounter { get; private set; }
        public PerformanceCounter TransactionRollbackFinishedCounter { get; private set; }

        public YawnCounters(string InstanceName)
        {
            if (SetupCounters())
            {
                while (!PerformanceCounterCategory.Exists(CounterCategoryName))
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }

            this.InitializeCounter = new PerformanceCounter(CounterCategoryName, InitializeCounterName, InstanceName, false);
            this.InitializeCounter.RawValue = 0;

            this.TransactionCommitStartCounter = new PerformanceCounter(CounterCategoryName, TransactionCommitStartCounterName, InstanceName, false);
            this.TransactionCommitStartCounter.RawValue = 0;

            this.TransactionCommitFailedCounter = new PerformanceCounter(CounterCategoryName, TransactionCommitFailedCounterName, InstanceName, false);
            this.TransactionCommitFailedCounter.RawValue = 0;

            this.TransactionCommitFinishedCounter = new PerformanceCounter(CounterCategoryName, TransactionCommitFinishedCounterName, InstanceName, false);
            this.TransactionCommitFinishedCounter.RawValue = 0;

            this.TransactionRollbackStartCounter = new PerformanceCounter(CounterCategoryName, TransactionRollbackStartCounterName, InstanceName, false);
            this.TransactionRollbackStartCounter.RawValue = 0;

            this.TransactionRollbackFailedCounter = new PerformanceCounter(CounterCategoryName, TransactionRollbackFailedCounterName, InstanceName, false);
            this.TransactionRollbackFailedCounter.RawValue = 0;

            this.TransactionRollbackFinishedCounter = new PerformanceCounter(CounterCategoryName, TransactionRollbackFinishedCounterName, InstanceName, false);
            this.TransactionRollbackFinishedCounter.RawValue = 0;
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

                CounterCreationData transactionCommitStartCount = new CounterCreationData();
                transactionCommitStartCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                transactionCommitStartCount.CounterName = TransactionCommitStartCounterName;
                counterDataCollection.Add(transactionCommitStartCount);

                CounterCreationData transactionCommitFailedCount = new CounterCreationData();
                transactionCommitFailedCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                transactionCommitFailedCount.CounterName = TransactionCommitFailedCounterName;
                counterDataCollection.Add(transactionCommitFailedCount);


                CounterCreationData transactionCommitFinishedCount = new CounterCreationData();
                transactionCommitFinishedCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                transactionCommitFinishedCount.CounterName = TransactionCommitFinishedCounterName;
                counterDataCollection.Add(transactionCommitFinishedCount);

                CounterCreationData transactionRollbackStartCount = new CounterCreationData();
                transactionRollbackStartCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                transactionRollbackStartCount.CounterName = TransactionRollbackStartCounterName;
                counterDataCollection.Add(transactionRollbackStartCount);

                CounterCreationData transactionRollbackFailedCount = new CounterCreationData();
                transactionRollbackFailedCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                transactionRollbackFailedCount.CounterName = TransactionRollbackFailedCounterName;
                counterDataCollection.Add(transactionRollbackFailedCount);

                CounterCreationData transactionRollbackFinishedCount = new CounterCreationData();
                transactionRollbackFinishedCount.CounterType = PerformanceCounterType.RateOfCountsPerSecond32;
                transactionRollbackFinishedCount.CounterName = TransactionRollbackFinishedCounterName;
                counterDataCollection.Add(transactionRollbackFinishedCount);

                PerformanceCounterCategory.Create(CounterCategoryName,
                "Perfomance counters for YawnDB.",
                PerformanceCounterCategoryType.MultiInstance, counterDataCollection);

                return true;
            }

            return false;
        }
    }
}
