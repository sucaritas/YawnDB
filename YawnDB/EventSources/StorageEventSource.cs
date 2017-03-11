namespace YawnDB.EventSources
{
    using System.Diagnostics.Tracing;

    public static class StorageEvents
    {
        public const int InitializeStart = 1;
        public const int InitializeFinish = 2;

        public const int RecordWriteStart = 3;
        public const int RecordWriteFinish = 4;

        public const int RecordReadFromCache = 5;
        public const int RecordReadStart = 6;
        public const int RecordReadFinish = 7;

        public const int RecordSerializeStart = 8;
        public const int RecordSerializeFinish = 9;

        public const int RecordDeserializeStart = 10;
        public const int RecordDeserializeFinish = 11;

        public const int RecordDeleteStart = 11;
        public const int RecordDeleteFinish = 12;

        public const int IndexingStart = 13;
        public const int IndexingFinish = 14;
    }

    [EventSource(Name = "StorageEventSource", Guid = "3dad70c1-5914-4b83-a074-f451de547eca")]
    public class StorageEventSource : EventSource
    {
        public static StorageEventSource Log = new StorageEventSource();

        [Event(StorageEvents.InitializeStart, Level = EventLevel.Informational)]
        public void InitializeStart(string storageName)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.InitializeStart, storageName);
            }
        }

        [Event(StorageEvents.InitializeFinish, Level = EventLevel.Informational, Message = "Finished Initializing {0} in {1}")]
        public void InitializeFinish(string storageName)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.InitializeFinish, storageName);
            }
        }

        [Event(StorageEvents.RecordWriteStart, Level = EventLevel.Verbose)]
        public void RecordWriteStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordWriteStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordWriteFinish, Level = EventLevel.Verbose, Message = "Finished Record Write for {1} in {2} in storage {0}")]
        public void RecordWriteFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordWriteFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordReadFromCache, Level = EventLevel.Verbose)]
        public void RecordReadFromCahe(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordReadFromCache, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordReadStart, Level = EventLevel.Verbose)]
        public void RecordReadStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordReadStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordReadFinish, Level = EventLevel.Verbose)]
        public void RecordReadFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordReadFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordSerializeStart, Level = EventLevel.Verbose)]
        public void RecordSerializeStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordSerializeStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordSerializeFinish, Level = EventLevel.Verbose)]
        public void RecordSerializeFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordSerializeFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordDeserializeStart, Level = EventLevel.Verbose)]
        public void RecordDeserializeStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordDeserializeStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordDeserializeFinish, Level = EventLevel.Verbose)]
        public void RecordDeserializeFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordDeserializeFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordDeleteStart, Level = EventLevel.Verbose)]
        public void RecordDeleteStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordDeleteStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordDeleteFinish, Level = EventLevel.Verbose)]
        public void RecordDeleteFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.RecordDeleteFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.IndexingStart, Level = EventLevel.Informational)]
        public void IndexingStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.IndexingStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.IndexingFinish, Level = EventLevel.Informational)]
        public void IndexingFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                WriteEvent(StorageEvents.IndexingFinish, storageName + ":" + recordID);
            }
        }
    }
}
