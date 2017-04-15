// <copyright file="StorageEventSource.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.EventSources
{
    using System.Diagnostics.Tracing;

    [EventSource(Name = "StorageEventSource", Guid = "3dad70c1-5914-4b83-a074-f451de547eca")]
    public class StorageEventSource : EventSource
    {
        public static StorageEventSource Log { get; set; } = new StorageEventSource();

        [Event(StorageEvents.InitializeStart, Level = EventLevel.Informational)]
        public void InitializeStart(string storageName)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.InitializeStart, storageName);
            }
        }

        [Event(StorageEvents.InitializeFinish, Level = EventLevel.Informational, Message = "Finished Initializing {0} in {1}")]
        public void InitializeFinish(string storageName)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.InitializeFinish, storageName);
            }
        }

        [Event(StorageEvents.RecordWriteStart, Level = EventLevel.Verbose)]
        public void RecordWriteStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordWriteStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordWriteFinish, Level = EventLevel.Verbose, Message = "Finished Record Write for {1} in {2} in storage {0}")]
        public void RecordWriteFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordWriteFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordReadFromCache, Level = EventLevel.Verbose)]
        public void RecordReadFromCahe(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordReadFromCache, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordReadStart, Level = EventLevel.Verbose)]
        public void RecordReadStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordReadStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordReadFinish, Level = EventLevel.Verbose)]
        public void RecordReadFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordReadFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordSerializeStart, Level = EventLevel.Verbose)]
        public void RecordSerializeStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordSerializeStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordSerializeFinish, Level = EventLevel.Verbose)]
        public void RecordSerializeFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordSerializeFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordDeserializeStart, Level = EventLevel.Verbose)]
        public void RecordDeserializeStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordDeserializeStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordDeserializeFinish, Level = EventLevel.Verbose)]
        public void RecordDeserializeFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordDeserializeFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordDeleteStart, Level = EventLevel.Verbose)]
        public void RecordDeleteStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordDeleteStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.RecordDeleteFinish, Level = EventLevel.Verbose)]
        public void RecordDeleteFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.RecordDeleteFinish, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.IndexingStart, Level = EventLevel.Informational)]
        public void IndexingStart(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.IndexingStart, storageName + ":" + recordID);
            }
        }

        [Event(StorageEvents.IndexingFinish, Level = EventLevel.Informational)]
        public void IndexingFinish(string storageName, long recordID)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(StorageEvents.IndexingFinish, storageName + ":" + recordID);
            }
        }
    }
}
