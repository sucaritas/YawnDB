namespace YawnDB.Storage.OneFilePerRecord
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using YawnDB.Interfaces;
    using YawnDB.Index;
    using System.Linq;
    using System.Runtime.Caching;
    using System.IO;
    using System.Reflection;
    using Bond;
    using Bond.Protocols;
    using Bond.IO.Unsafe;
    using YawnDB.Utils;

    /*
     Saves records for schema T,
     This storage saves them to disk, and can maintain some records in memory.
     The expected disk structure is as follows

    YAWN_DATA_FOLDER_ROOT (obtained through the yawn site)
    |
    +----FULLNAME_OF_SCHEMA_<T>
         |
         +----PARTITION_ID
              |
              +----SEGMENT_ID
                   |
                   +----RECORD_ID.BIN
     */

    public class OnFilePerRecordStorage<T> : IStorageOf<T>, IStorage where T : YawnSchema
    {
        private IYawn yawnSite;
        public IDictionary<string, IIndex> Indicies { get; private set; } = new Dictionary<string, IIndex>();

        private static MemoryCache cache;

        public Type SchemaType { get; } = typeof(T);

        private Deserializer<CompactBinaryReader<InputBuffer>> SchemaDeserializer = new Deserializer<CompactBinaryReader<InputBuffer>>(typeof(T));
        private Serializer<CompactBinaryWriter<OutputBuffer>> SchemaSerializer = new Serializer<CompactBinaryWriter<OutputBuffer>>(typeof(T));

        private long NumberOfPartitions;

        private long NumberOfSegmentsInPartition;

        private string PathToSchemaFolder;

        private ulong NextIndex = 0;

        private Dictionary<ulong, Dictionary<ulong, ulong>> RecordCounts = new Dictionary<ulong, Dictionary<ulong, ulong>>();

        public OnFilePerRecordStorage(IYawn yawnSite, int numberOfPartitions = 100, int numberOfSegmentsInPartition = 100)
        {
            this.yawnSite = yawnSite;
            this.NumberOfPartitions = numberOfPartitions;
            this.NumberOfSegmentsInPartition = numberOfSegmentsInPartition;
            cache = new MemoryCache(yawnSite.DatabaseName + this.GetType().FullName);
            init();
        }

        public void init()
        {
            var typeNameNormilized = this.SchemaType.Namespace + "." + this.SchemaType.Name;
            if (this.SchemaType.IsGenericType)
            {
                typeNameNormilized += "[";
                foreach (var genericArgument in this.SchemaType.GetGenericArguments())
                {
                    typeNameNormilized += genericArgument.Namespace + "." + genericArgument.Name;
                }
                typeNameNormilized += "]";
            }

            this.PathToSchemaFolder = Path.Combine(yawnSite.DefaultStoragePath, typeNameNormilized);
            if (!Directory.Exists(this.PathToSchemaFolder))
            {
                Directory.CreateDirectory(this.PathToSchemaFolder);
            }

            var storageFilePath = Path.Combine(this.PathToSchemaFolder, "StorageFile.bin");
            Indicies = Utilities.GetIndeciesFromSchema(this.SchemaType);
            SetupPartitions();
            var directories = Directory.GetDirectories(Path.GetDirectoryName(storageFilePath), "Partition_*");
            RereshPartitions(directories);
        }

        private void SetupPartitions()
        {
            for (long p = 0; p < this.NumberOfPartitions; p++)
            {
                for (long s = 0; s < this.NumberOfSegmentsInPartition; s++)
                {
                    var segmentDirectory = Path.Combine(this.PathToSchemaFolder, "Partition_" + p, "Segment_" + s);
                    if (!Directory.Exists(segmentDirectory))
                    {
                        Directory.CreateDirectory(segmentDirectory);
                    }
                }
            }
        }

        private void RereshPartitions(IEnumerable<string> partitionPaths)
        {
            foreach (var partitionPath in partitionPaths)
            {
                var directories = Directory.GetDirectories(partitionPath, "Segment_*");

                var splitParts = Path.GetFileName(partitionPath).Split('_');
                ulong partitionID = ulong.Parse(splitParts[splitParts.Length - 1]);

                if (!this.RecordCounts.ContainsKey(partitionID))
                {
                    this.RecordCounts[partitionID] = new Dictionary<ulong, ulong>();
                }

                RefreshSegments(directories, partitionID);
            }
        }

        private void RefreshSegments(IEnumerable<string> segmentPaths, ulong partitionID)
        {
            foreach (var segmentPath in segmentPaths)
            {
                var directories = Directory.GetFiles(segmentPath, "Record_*");

                var splitParts = Path.GetFileName(segmentPath).Split('_');
                ulong segmentID = ulong.Parse(splitParts[splitParts.Length - 1]);

                if (!this.RecordCounts[partitionID].ContainsKey(segmentID))
                {
                    this.RecordCounts[partitionID][segmentID] = 0;
                }

                RefreshRecords(directories, partitionID, segmentID);
            }
        }

        private void RefreshRecords(IEnumerable<string> recordPaths, ulong partitionID, ulong segmentID)
        {
            foreach (var recordPath in recordPaths)
            {
                LoadRecord(recordPath, partitionID, segmentID);
            }
        }

        private bool LoadRecord(ulong partitionID, ulong segmentID, ulong recordID)
        {
            return LoadRecord(Path.Combine(this.PathToSchemaFolder,
                                           "Partition_" + partitionID,
                                           "Segment_" + segmentID,
                                           "Record_" + recordID),
                              partitionID,
                              segmentID);
        }

        private bool LoadRecord(string recordPath, ulong partitionID, ulong segmentID)
        {
            var recordID = Path.GetFileNameWithoutExtension(recordPath);
            var splitParts = recordID.Split('_');
            recordID = splitParts[splitParts.Length - 1];

            var cacheID = "P" + partitionID + "_S" + segmentID + "_R" + recordID;
            cache.Remove(cacheID);

            var record = ReadRecord(recordPath);

            if (record == null)
            {
                return false;
            }

            cache.Set(cacheID, record, new CacheItemPolicy());
            OneFilePerRecordLocation storageLocation = new OneFilePerRecordLocation()
            {
                Partition = partitionID,
                Segment = segmentID,
                Index = ulong.Parse(recordID)
            };

            if (storageLocation.Index > this.NextIndex)
            {
                this.NextIndex = storageLocation.Index + 1;
            }

            this.RecordCounts[partitionID][segmentID]++;
            UpdateIndecies(storageLocation, record);
            return true;
        }

        private void UpdateIndecies(OneFilePerRecordLocation storageLocation, T record)
        {
            foreach (IIndex index in Indicies.Values)
            {
                index.SetIndex(record, storageLocation);
            }
        }

        private T ReadRecord(string recordPath)
        {
            try
            {

                var input = new InputBuffer(File.ReadAllBytes(recordPath));
                var reader = new CompactBinaryReader<InputBuffer>(input);

                return SchemaDeserializer.Deserialize<T>(reader);

            }
            catch { }

            return null;
        }

        public ulong GetNextID()
        {
            return this.NextIndex++;
        }
        public IStorageLocation SaveRecord(T instanceToSave)
        {
            var locationForRecord = GetSaveLocation(instanceToSave);
            if (WriteSchemaToFile(instanceToSave, this.GetPathFromStorageLocation(locationForRecord)))
            {
                AddToRecordCount(locationForRecord);
                return locationForRecord;
            }

            return null;
        }

        private void AddToRecordCount(OneFilePerRecordLocation sl)
        {
            if (!this.RecordCounts.ContainsKey(sl.Partition))
            {
                this.RecordCounts.Add(sl.Partition, new Dictionary<ulong, ulong>());
            }

            if (!this.RecordCounts[sl.Partition].ContainsKey(sl.Segment))
            {
                this.RecordCounts[sl.Partition].Add(sl.Segment, 0);
            }

            this.RecordCounts[sl.Partition][sl.Segment]++;
        }


        private object writeLock = new object();
        private bool WriteSchemaToFile(T instance, string path)
        {
            try
            {
                lock (writeLock)
                {
                    var output = new OutputBuffer();
                    var writer = new CompactBinaryWriter<OutputBuffer>(output);
                    this.SchemaSerializer.Serialize(instance, writer);
                    File.WriteAllBytes(Path.Combine(path, "Record_" + instance.Id + ".bin"), output.Data.Array);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private OneFilePerRecordLocation GetSaveLocation(T instanceToSave)
        {
            var keyIndex = Indicies["YawnKeyIndex"] as KeyIndex;
            if (keyIndex == null)
            {
                return null;
            }

            var existingLocation = keyIndex.GetLocationForInstance(instanceToSave) as OneFilePerRecordLocation;

            return existingLocation ?? GetNextStorageLocation();
        }

        private OneFilePerRecordLocation GetNextStorageLocation()
        {
            ulong minCount = ulong.MaxValue;
            ulong partition = 0;
            ulong segment = 0;
            foreach (var ps in RecordCounts)
            {
                foreach (var sc in ps.Value)
                {
                    if (minCount > sc.Value)
                    {
                        minCount = sc.Value;
                        partition = ps.Key;
                        segment = sc.Key;
                    }
                }
            }

            return new OneFilePerRecordLocation() { Partition = partition, Segment = segment };
        }

        private string GetPathFromStorageLocation(OneFilePerRecordLocation location)
        {
            if (location == null)
            {
                return null;
            }

            return Path.Combine(this.PathToSchemaFolder, "Partition_" + location.Partition, "Segment_" + location.Segment);
        }

        public IEnumerable<T> GetRecords(IEnumerable<IStorageLocation> recordsToPull)
        {
            List<T> results = new List<T>();
            for (int i = 0; i < 10; i++)
            {
                results.Add((T)Activator.CreateInstance(this.SchemaType, new object[] { }));
            }

            return results.AsEnumerable();
        }

        public T CreateRecord()
        {
            var record = Activator.CreateInstance(typeof(T)) as YawnSchema;
            record.Id = this.NextIndex++;
            return record as T;
        }

        public IEnumerable<T> GetAllRecords()
        {
            for(int p = 0; p< NumberOfPartitions; p++)
            {
                for(int s = 0; s< NumberOfSegmentsInPartition; s++)
                {
                    var segmentPath = Path.Combine(this.PathToSchemaFolder, "Partition_" + p, "Segment_" + s);
                    var files = Directory.GetFiles(segmentPath, "Record_*");

                    foreach(var filePath in files)
                    {
                        var input = new InputBuffer(File.ReadAllBytes(filePath));
                        var reader = new CompactBinaryReader<InputBuffer>(input);

                        yield return SchemaDeserializer.Deserialize<T>(reader);
                    }
                }
            }
        }

        public bool DeleteRecord(T instanceToSave)
        {
            return true;
        }

        public void ReIndexStorage(IList<IIndex> needReindexing)
        {
        }
    }
}
