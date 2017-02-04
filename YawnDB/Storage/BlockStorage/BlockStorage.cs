namespace YawnDB.Storage.BlockStorage
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    /*
    BlockStorage file format:

    Block
    ╔═══════════════════════════════════════════════════════════════╗ ─┐
    ║ Header                                                        ║  │
    ║ ┌───────────────────┬──────────────────────┐ ─┐               ║  │
    ║ │ BlockProperties   │ 1 Byte (1bit flags)  │  │               ║  │
    ║ ├───────────────────┼──────────────────────┤  │               ║  │
    ║ │ NextBlockLocation │ 8 Bytes (1 long int) │  ╞══ 17 bytes    ║  │
    ║ ├───────────────────┼──────────────────────┤  │   Header size ║  │
    ║ │ RecordSize        │ 8 Bytes (1 long int) │  │               ║  ╞══ Block Size
    ║ └───────────────────┴──────────────────────┘ ─┘               ║  │
    ║ Bits in block                                                 ║  │
    ║ ┌──────────────────────────────────────────┐                  ║  │
    ║ │░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░│                  ║  │
    ║ │░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░│                  ║  │ 
    ║ │░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░│                  ║  │
    ║ │░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░│                  ║  │
    ║ └──────────────────────────────────────────┘                  ║  │
    ╚═══════════════════════════════════════════════════════════════╝ ─┘
    */

    using System;
    using System.Collections.Generic;
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
    using YawnDB.Exceptions;
    using System.Reflection;

    public class BlockStorage<T> : IStorageOf<T> where T : YawnSchema
    {
        private IYawn YawnSite;

        public StorageState State { get; private set; } = StorageState.Closed;

        public IDictionary<string, IIndex> Indicies { get; private set; } = new Dictionary<string, IIndex>();

        public Type SchemaType { get; } = typeof(T);

        public int BlockSize { get; }

        public long Capacity { get; private set; }

        public int NumberOfBufferBlocks { get; set; }

        public string FilePath { get; private set; }

        public string FullStorageName { get; }

        private MemoryCache Cache;

        private string TypeNameNormilized;

        private string PathToSchemaFolder;

        private MemoryMappedFile MappedFile;

        private FreeBlocks FreeBlocks;

        private Deserializer<CompactBinaryReader<InputBuffer>> SchemaDeserializer = new Deserializer<CompactBinaryReader<InputBuffer>>(typeof(T));

        private Serializer<CompactBinaryWriter<OutputBuffer>> SchemaSerializer = new Serializer<CompactBinaryWriter<OutputBuffer>>(typeof(T));

        private Cloner<T> Cloner = new Cloner<T>(typeof(T));

        private object ResizeLock = new object();

        private object AutoIdLock = new object();

        private long NextIndex = 0;

        private StorageCounters PerfCounters;

        private BlockStorageLocker RecordLocker;

        private List<PropertyInfo> ReferencingProperties = new List<PropertyInfo>();

        public BlockStorage(IYawn yawnSite, int blockSize = 512, int numberOfBufferBlocks = 10000)
        {
            this.YawnSite = yawnSite;
            this.BlockSize = blockSize;
            this.NumberOfBufferBlocks = numberOfBufferBlocks;

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
            Cache = new MemoryCache(this.FullStorageName);
            this.PathToSchemaFolder = Path.Combine(this.YawnSite.DefaultStoragePath, this.TypeNameNormilized);
            if (!Directory.Exists(this.PathToSchemaFolder))
            {
                Directory.CreateDirectory(this.PathToSchemaFolder);
            }

            this.FilePath = Path.Combine(this.PathToSchemaFolder, this.TypeNameNormilized + ".ydb");
        }

        public void Open()
        {
            this.PerfCounters = new StorageCounters(this.FullStorageName);
            this.RecordLocker = new BlockStorageLocker(this.PerfCounters.WriteContentionCounter);
            StorageEventSource.Log.InitializeStart("BlockStorage: " + this.FullStorageName);

            bool fileExist = File.Exists(this.FilePath);

            var minBlockSize = BlockHelpers.GetHeaderSize() + 1;
            if (this.BlockSize < minBlockSize)
            {
                throw new InvalidDataException("BlockSize cannot be lower than " + minBlockSize + " bytes");
            }

            if (fileExist)
            {
                this.Capacity = (new FileInfo(this.FilePath)).Length;
                if ((this.Capacity % this.BlockSize) != 0)
                {
                    throw new DataMisalignedException("File size in bytes for " + this.FilePath + " is missaligned for block size " + this.BlockSize);
                }
            }
            else
            {
                this.Capacity = this.NumberOfBufferBlocks * this.BlockSize;
            }

            this.MappedFile = MemoryMappedFile.CreateFromFile(this.FilePath, FileMode.OpenOrCreate, this.TypeNameNormilized, this.Capacity, MemoryMappedFileAccess.ReadWrite);

            var freeBlocksFile = Path.Combine(this.PathToSchemaFolder, "Freeblocks.bin");
            FreeBlocks = new FreeBlocks(freeBlocksFile);
            if (!fileExist)
            {
                FreeBlocks.AddFreeBlockRange(0, this.NumberOfBufferBlocks, this.BlockSize);
            }
            else if (!File.Exists(freeBlocksFile))
            {
                using (var MapAccessor = this.MappedFile.CreateViewAccessor(0, this.Capacity, MemoryMappedFileAccess.Read))
                {
                    FreeBlocks.ScanFreeBlocksFromMap(MapAccessor, this.Capacity, this.BlockSize);
                    MapAccessor.Flush();
                    MapAccessor.Dispose();
                }
            }

            this.Indicies = Utilities.GetIndeciesFromSchema(this.SchemaType, typeof(BlockStorageLocation));
            List<IIndex> needReindexing = new List<IIndex>();

            StorageEventSource.Log.IndexingStart(this.FullStorageName);
            foreach (var index in this.Indicies)
            {
                if (!index.Value.Initialize(this.PathToSchemaFolder, true))
                {
                    needReindexing.Add(index.Value);
                }
            }

            ReIndexStorage(needReindexing);

            var proterties = typeof(T).GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in proterties)
            {
                if (typeof(IReference).IsAssignableFrom(prop.PropertyType))
                {
                    ReferencingProperties.Add(prop);
                }
            }

            StorageEventSource.Log.IndexingFinish(this.FullStorageName);
            StorageEventSource.Log.InitializeFinish(this.FullStorageName);
            this.PerfCounters.InitializeCounter.Increment();
            this.State = StorageState.Open;
        }

        public async Task<IStorageLocation> SaveRecord(T inputInstance)
        {
            if(this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to write to database '{this.YawnSite.DatabaseName}' which is closed");
            }

            var instance = this.Cloner.Clone<T>(inputInstance);
            var output = new OutputBuffer();
            var writer = new CompactBinaryWriter<OutputBuffer>(output);
            this.SchemaSerializer.Serialize(instance, writer);
            int RecordSize = output.Data.Count;
            int realBlockSize = this.BlockSize - BlockHelpers.GetHeaderSize();
            var numberOfBlocksNeeded = RecordSize / realBlockSize;
            numberOfBlocksNeeded += (RecordSize % realBlockSize) == 0 ? 0 : 1;
            long[] addresses;

            var keyIndex = Indicies["YawnKeyIndex"];
            var existingLocation = keyIndex.GetLocationForInstance(instance) as BlockStorageLocation;

            List<long> existingAddress = new List<long>();
            if (existingLocation != null)
            {
                existingAddress = GetRecordBlockAddresses(existingLocation);
                if (existingAddress.Count < numberOfBlocksNeeded)
                {
                    existingAddress.AddRange(GetFreeBlocks(numberOfBlocksNeeded - existingAddress.Count));
                }
            }

            addresses = GetFreeBlocks(numberOfBlocksNeeded);

            bool processingFirst = true;
            List<Block> blocksToWrite = new List<Block>();
            BlockStorageLocation location = new BlockStorageLocation();

            for (int i = 0; i < addresses.Length; i++)
            {
                byte blockProperties = BlockProperties.InUse;
                if (processingFirst)
                {
                    blockProperties |= BlockProperties.IsFirstBlockInRecord;
                    if (addresses.Length == 1)
                    {
                        blockProperties |= BlockProperties.IsLastBlockInRecord;
                    }

                    location.Address = addresses[i];
                }
                else
                {
                    blocksToWrite[i - 1].Header.NextBlockLocation = addresses[i];
                    if (addresses.Length == (i + 1))
                    {
                        blockProperties |= BlockProperties.IsLastBlockInRecord;
                    }
                }

                processingFirst = false;
                blocksToWrite.Add(new Block()
                {
                    Header = new BlockHeader()
                    {
                        BlockProperties = blockProperties,
                        NextBlockLocation = 0,
                        RecordSize = RecordSize
                    },
                    Address = addresses[i],
                    BlockBytes = output.Data.Array.Skip(i * realBlockSize).Take(realBlockSize).ToArray()
                });
            }

            BlockStorageSyncLock writeLock;
            lock (this.ResizeLock)
            {
                writeLock = this.RecordLocker.LockRecord(blocksToWrite[0].Address);
            }

            T existingInstance = null;
            lock (writeLock)
            {
                existingInstance = ReadRecord(existingLocation).Result;
                this.RecordLocker.WaitForRecord(blocksToWrite[0].Address, 1);
                List<Task<bool>> tasks = new List<Task<bool>>();
                foreach (var blk in blocksToWrite)
                {
                    this.WriteBlock(blk);
                }
            }

            this.RecordLocker.UnLockRecord(blocksToWrite[0].Address);

            if (existingLocation != null)
            {
                Cache.Remove(existingLocation.Address.ToString());
            }

            Cache.Set(blocksToWrite[0].Address.ToString(), instance, new CacheItemPolicy());
            UpdateIndeciesForInstance(existingInstance, instance, location as StorageLocation);
            existingAddress.Select(x => FreeBlock(x));
            this.PerfCounters.RecordWriteCounter.Increment();
            return location as StorageLocation;
        }

        private bool FreeBlock(long blockLocation)
        {
            using (var viewWriter = this.MappedFile.CreateViewAccessor(blockLocation, this.BlockSize, MemoryMappedFileAccess.ReadWrite))
            {
                var header = new BlockHeader() { BlockProperties = 0, NextBlockLocation = 0, RecordSize = 0 };
                var headerSize = BlockHelpers.GetHeaderSize();
                viewWriter.Write<BlockHeader>(0, ref header);
                viewWriter.Flush();
                viewWriter.Dispose();
            }

            return true;
        }

        private List<long> GetRecordBlockAddresses(BlockStorageLocation blockLocation)
        {
            long location = blockLocation.Address;
            bool lastBlock = false;
            List<long> addresses = new List<long>() { location };

            while (!lastBlock)
            {
                using (var readerView = this.MappedFile.CreateViewAccessor(location, this.BlockSize))
                {
                    BlockHeader header;
                    readerView.Read<BlockHeader>(0, out header);
                    lastBlock = (header.BlockProperties & BlockProperties.IsLastBlockInRecord) != 0;
                    if (!lastBlock)
                    {
                        addresses.Add(header.NextBlockLocation);
                    }

                    readerView.Dispose();
                }
            }

            return addresses;
        }

        private bool WriteBlock(Block block)
        {
            using (var viewWriter = this.MappedFile.CreateViewAccessor(block.Address, this.BlockSize, MemoryMappedFileAccess.ReadWrite))
            {
                var headerSize = BlockHelpers.GetHeaderSize();
                viewWriter.Write<BlockHeader>(0, ref block.Header);
                viewWriter.WriteArray(headerSize, block.BlockBytes, 0, this.BlockSize - headerSize);
                viewWriter.Flush();
                viewWriter.Dispose();
            }

            return true;
        }

        private long[] GetFreeBlocks(int numberOfBlocks)
        {
            long address;
            List<long> addresses = new List<long>();
            for (int i = 0; i < numberOfBlocks; i++)
            {
                if (FreeBlocks.PopFreeBlock(out address))
                {
                    addresses.Add(address);
                }
                else
                {
                    ResizeFile(this.NumberOfBufferBlocks);
                    if (FreeBlocks.PopFreeBlock(out address))
                    {
                        addresses.Add(address);
                    }
                    else
                    {
                        throw new AccessViolationException("No free blocks after resize!!");
                    }
                }
            }

            return addresses.ToArray();
        }

        public void ResizeFile(int nuberOfBlocks)
        {
            // Allreaders must be closed, the Memory Mapped file must be closed
            // therfore we must be mutualy exclusive form reads
            lock (this.ResizeLock)
            {
                this.RecordLocker.WaitForAllReaders();
                var firstAddressInNewArea = this.Capacity;
                this.Capacity += this.NumberOfBufferBlocks * this.BlockSize;

                // Close the mapped file an reopen with add capacity
                this.MappedFile.Dispose();
                this.MappedFile = MemoryMappedFile.CreateFromFile(this.FilePath, FileMode.OpenOrCreate, this.TypeNameNormilized, this.Capacity, MemoryMappedFileAccess.ReadWrite);

                FreeBlocks.AddFreeBlockRange(firstAddressInNewArea, this.NumberOfBufferBlocks, this.BlockSize);

                this.PerfCounters.ResizeCounter.Increment();
            }
        }

        private void UpdateIndeciesForInstance(T oldRecord, T newRecord, StorageLocation newLocation)
        {
            this.PerfCounters.IndexingCounter.Increment();
            foreach (var index in this.Indicies)
            {
                index.Value.UpdateIndex(oldRecord, newRecord, newLocation);
            }
        }

        private void DeleteIndeciesForInstance(T instance)
        {
            this.PerfCounters.IndexingCounter.Increment();
            foreach (var index in this.Indicies)
            {
                index.Value.DeleteIndex(instance);
            }
        }

        public async Task<IEnumerable<TE>> GetRecordsAsync<TE>(IEnumerable<IStorageLocation> recordsToPull) where TE : YawnSchema
        {
            if (recordsToPull == null)
            {
                return Enumerable.Empty<TE>();
            }

            List<Task<T>> pendingRecords = new List<Task<T>>();
            foreach (var location in recordsToPull)
            {
                if (location == null)
                {
                    continue;
                }

                pendingRecords.Add(this.ReadRecord(location));
            }

            Task.WaitAll(pendingRecords.ToArray());

            return pendingRecords.Where(x => x.Result != null).Select(x => x.Result as TE);
        }

        public async Task<T> ReadRecord(IStorageLocation fromLocation)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to read from database '{this.YawnSite.DatabaseName}' which is closed");
            }

            BlockStorageLocation blockStorageLocation = fromLocation as BlockStorageLocation;
            if (blockStorageLocation == null)
            {
                return null;
            }

            long location = blockStorageLocation.Address;
            long FirstLocation = location;
            var cacheInstance = Cache.Get(location.ToString());
            if (cacheInstance != null)
            {
                return Cloner.Clone<T>(cacheInstance as T);
            }

            lock (this.ResizeLock)
            {
                this.RecordLocker.LockRecord(FirstLocation);
            }

            bool lastBlock = false;
            byte[] buffer = null;
            int i = 0;
            int bytesReadSofar = 0;

            while (!lastBlock)
            {
                using (var readerView = this.MappedFile.CreateViewAccessor(location, this.BlockSize))
                {
                    BlockHeader header;
                    readerView.Read<BlockHeader>(0, out header);
                    lastBlock = (header.BlockProperties & BlockProperties.IsLastBlockInRecord) != 0;
                    location = header.NextBlockLocation;
                    if (i == 0)
                    {
                        buffer = new byte[header.RecordSize];
                    }

                    var bytesInBlock = BytesOnBlock(header.RecordSize, lastBlock);
                    readerView.ReadArray(BlockHelpers.GetHeaderSize(), buffer, bytesReadSofar, bytesInBlock);
                    bytesReadSofar += bytesInBlock;
                }

                i++;
            }

            this.RecordLocker.UnLockRecord(FirstLocation);

            var input = new InputBuffer(buffer.ToArray());
            var reader = new CompactBinaryReader<InputBuffer>(input);
            T instance = SchemaDeserializer.Deserialize<T>(reader);
            Cache.Set(location.ToString(), instance, new CacheItemPolicy());
            this.PerfCounters.RecordReadCounter.Increment();
            return PropagateSite(Cloner.Clone<T>(instance));
        }

        private int BytesOnBlock(long recordSize, bool isLastBlock)
        {
            var blockSize = this.BlockSize - BlockHelpers.GetHeaderSize();
            if ((recordSize < blockSize) && isLastBlock)
            {
                return (int)recordSize;
            }
            else if (isLastBlock)
            {
                return (int)(recordSize % blockSize);
            }

            return blockSize;
        }

        public async Task<IEnumerable<TE>> GetAllRecordsAsync<TE>() where TE : YawnSchema
        {
            return await GetRecordsAsync<TE>(Indicies["YawnKeyIndex"].EnumerateAllLocations());
        }

        public IEnumerable<TE> GetRecords<TE>(IEnumerable<IStorageLocation> recordsToPull) where TE : YawnSchema
        {
            if (recordsToPull == null)
            {
                yield break;
            }

            foreach (var location in recordsToPull)
            {
                if (location == null)
                {
                    continue;
                }

                yield return this.ReadRecord(location).Result as TE;
            }

            yield break;
        }

        public IEnumerable<TE> GetAllRecords<TE>() where TE : YawnSchema
        {
            return GetRecords<TE>(Indicies["YawnKeyIndex"].EnumerateAllLocations().ToArray());
        }

        public async Task<T> CreateRecord()
        {
            var record = Activator.CreateInstance(typeof(T)) as YawnSchema;
            record.Id = this.GetNextID();
            return PropagateSite(record as T);
        }

        public long GetNextID()
        {
            lock (AutoIdLock)
            {
                Interlocked.Increment(ref this.NextIndex);
            }

            return this.NextIndex;
        }

        public bool DeleteRecord(T instance)
        {
            var keyIndex = Indicies["YawnKeyIndex"];
            var existingLocation = keyIndex.GetLocationForInstance(instance) as BlockStorageLocation;

            long location = existingLocation.Address;
            bool lastBlock = false;
            var blankHeader = new BlockHeader();

            lock (this.ResizeLock)
            {
            }

            lock (this.RecordLocker.LockRecord(location))
            {
                while (!lastBlock)
                {
                    using (var deleteView = this.MappedFile.CreateViewAccessor(location, this.BlockSize))
                    {
                        BlockHeader header;
                        deleteView.Read<BlockHeader>(0, out header);
                        lastBlock = (header.BlockProperties & BlockProperties.IsLastBlockInRecord) != 0;
                        deleteView.Write<BlockHeader>(0, ref blankHeader);
                        deleteView.Dispose();

                        this.FreeBlocks.AddFreeBlock(location);
                        location = header.NextBlockLocation;
                    }
                }
            }

            this.RecordLocker.UnLockRecord(location);
            this.DeleteIndeciesForInstance(instance);
            this.Cache.Remove(existingLocation.Address.ToString());
            return true;
        }

        public void ReIndexStorage(IList<IIndex> needReindexing)
        {
            if (!needReindexing.Any())
            {
                return;
            }

            lock (this.ResizeLock)
            {
                using (var mapAccessor = this.MappedFile.CreateViewAccessor())
                {
                    int blocksInStorage = (int)(this.Capacity / this.BlockSize);
                    for (int i = 0; i < blocksInStorage; i++)
                    {
                        BlockHeader header;
                        long blockAddress = i * this.BlockSize;
                        mapAccessor.Read<BlockHeader>(blockAddress, out header);
                        if ((header.BlockProperties & BlockProperties.IsFirstBlockInRecord) != 0)
                        {
                            var location = new BlockStorageLocation() { Address = blockAddress };
                            T record = this.ReadRecord(location).Result;
                            if (this.NextIndex < record.Id)
                            {
                                this.NextIndex = record.Id;
                            }

                            foreach (var index in needReindexing)
                            {
                                index.SetIndex(record, location as StorageLocation);
                            }
                        }
                    }

                    mapAccessor.Dispose();
                }
            }
        }

        public void Close()
        {
            lock (this.ResizeLock)
            {
                this.RecordLocker.WaitForAllReaders();

                foreach (var index in this.Indicies)
                {
                    index.Value.Close(false);
                }

                this.FreeBlocks.SaveToFile();
                this.MappedFile.Dispose();
                this.State = StorageState.Closed;
            }
        }

        private T PropagateSite(T instance)
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
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
