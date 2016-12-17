namespace YawnDB.EventSources
{
    using System.Diagnostics.Tracing;

    public static class StorageEvents
    {
        public const int InitializeStart = 1;
        public const int InitializeFinish = 2;

        public const int RecordWriteStart = 3;
        public const int RecordWriteFinish = 4;
        public const int RecordReadStart = 5;
        public const int RecordReadFinish = 6;
        public const int RecordSerializeStart = 7;
        public const int RecordSerializeFinish = 8;
        public const int RecordDeserializeStart = 9;
        public const int RecordDeserializeFinish = 10;

        public const int IndexingStart = 11;
        public const int IndexingFinish = 12;
    }

    [EventSource(Name = "StorageEventSource", Guid = "3dad70c1-5914-4b83-a074-f451de547eca")]
    public class StorageEventSource : EventSource
    {
        public static StorageEventSource Log = new StorageEventSource();

        [Event(StorageEvents.InitializeStart, Level = EventLevel.Informational)]
        public void InitializeStart(string storageName)
        {
            WriteEvent(StorageEvents.InitializeStart, storageName);
        }

        [Event(StorageEvents.InitializeFinish, Level = EventLevel.Informational, Message = "Finished Initializing {0} in {1}")]
        public void InitializeFinish(string storageName)
        {
            WriteEvent(StorageEvents.InitializeFinish, storageName);
        }

        [Event(StorageEvents.RecordWriteStart, Level = EventLevel.Verbose)]
        public void RecordWriteStart(string storageName, string recordID)
        {
            WriteEvent(StorageEvents.RecordWriteStart, storageName + ":" + recordID);
        }

        [Event(StorageEvents.RecordWriteFinish, Level = EventLevel.Verbose, Message = "Finished Record Write for {1} in {2} in storage {0}")]
        public void RecordWriteFinish(string storageName, string recordID)
        {
            WriteEvent(StorageEvents.RecordWriteFinish, storageName + ":" + recordID);
        }

        [Event(StorageEvents.RecordReadStart, Level = EventLevel.Verbose)]
        public void RecordReadStart(string storageName, string recordID)
        {
            WriteEvent(StorageEvents.RecordReadStart, storageName + ":" + recordID);
        }

        [Event(StorageEvents.RecordReadFinish, Level = EventLevel.Verbose)]
        public void RecordReadFinish(string storageName, string recordID)
        {
            WriteEvent(StorageEvents.RecordReadFinish, storageName + ":" + recordID);
        }

        [Event(StorageEvents.RecordSerializeStart, Level = EventLevel.Verbose)]
        public void RecordSerializeStart(string storageName, string recordID)
        {
            WriteEvent(StorageEvents.RecordSerializeStart, storageName + ":" + recordID);
        }

        [Event(StorageEvents.RecordSerializeFinish, Level = EventLevel.Verbose)]
        public void RecordSerializeFinish(string storageName, string recordID)
        {
            WriteEvent(StorageEvents.RecordSerializeFinish, storageName + ":" + recordID);
        }

        [Event(StorageEvents.RecordDeserializeStart, Level = EventLevel.Verbose)]
        public void RecordDeserializeStart(string storageName, string recordID)
        {
            WriteEvent(StorageEvents.RecordDeserializeStart, storageName + ":" + recordID);
        }

        [Event(StorageEvents.RecordDeserializeFinish, Level = EventLevel.Verbose)]
        public void RecordDeserializeFinish(string storageName, string recordID)
        {
            WriteEvent(StorageEvents.RecordDeserializeFinish, storageName + ":" + recordID);
        }

        [Event(StorageEvents.IndexingStart, Level = EventLevel.Informational)]
        public void IndexingStart(string storageName)
        {
            WriteEvent(StorageEvents.IndexingStart, storageName);
        }

        [Event(StorageEvents.IndexingFinish, Level = EventLevel.Informational)]
        public void IndexingFinish(string storageName)
        {
            WriteEvent(StorageEvents.IndexingFinish, storageName);
        }
    }
}
