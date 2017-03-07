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

    public class BlockStorage<T> : IStorage where T : YawnSchema
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

        private int Resizers = 0;

        private object AutoIdLock = new object();

        private long NextIndex = 0;

        private StorageCounters PerfCounters;

        private StorageLocker RecordLocker;

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
            this.RecordLocker = new StorageLocker(this.PerfCounters.WriteContentionCounter);
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

            this.State = StorageState.Open;
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
        }

        public IStorageLocation InsertRecord(YawnSchema instanceToInsert)
        {
            return this.SaveRecord(instanceToInsert);
        }

        public IStorageLocation InsertRecord(YawnSchema instanceToInsert, ITransaction transaction)
        {
            return this.SaveRecord(instanceToInsert, transaction);
        }

        public IStorageLocation SaveRecord(YawnSchema inputInstance, ITransaction transaction)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to write to database '{this.YawnSite.DatabaseName}' which is closed");
            }

            if (!this.YawnSite.TransactionsEnabled && transaction != null)
            {
                throw new DatabaseTransactionsAreDisabled();
            }

            var isTransaction = transaction != null;
            var instance = this.Cloner.Clone<T>(inputInstance as T);
            var output = new OutputBuffer();
            var writer = new CompactBinaryWriter<OutputBuffer>(output);
            this.SchemaSerializer.Serialize(instance, writer);
            int RecordSize = output.Data.Count;
            int realBlockSize = this.BlockSize - BlockHelpers.GetHeaderSize();
            var numberOfBlocksNeeded = RecordSize / realBlockSize;
            numberOfBlocksNeeded += (RecordSize % realBlockSize) == 0 ? 0 : 1;
            long[] addresses = GetFreeBlocks(numberOfBlocksNeeded);

            bool processingFirst = true;
            List<Block> blocksToWrite = new List<Block>();
            BlockStorageLocation location = new BlockStorageLocation();
            byte blockProperties = BlockProperties.InUse;
            blockProperties |= isTransaction ? (byte)0 : BlockProperties.IsCommited;

            for (int i = 0; i < addresses.Length; i++)
            {
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

            using (var unlocker = new StorageUnlocker(this.RecordLocker.LockRecord(addresses[0], this.ResizeLock), this.RecordLocker))
            {
                // Write the prepared blocks
                foreach (var blk in blocksToWrite)
                {
                    this.WriteBlock(blk);
                }
            }

            var keyIndex = this.Indicies["YawnKeyIndex"];
            var existingLocation = keyIndex.GetLocationForInstance(instance) as BlockStorageLocation;
            T existingInstance = ReadRecord(existingLocation);
            List<long> existingAddresses = GetRecordBlockAddresses(existingLocation);

            if (isTransaction)
            {
                var transactionItem = new BlockTransactionItem();
                transactionItem.ItemAction = Transactions.TransactionAction.Update;
                transactionItem.SchemaType = SchemaType.AssemblyQualifiedName;
                transactionItem.OriginalAddresses = new LinkedList<long>(existingAddresses);
                transactionItem.OldInstance = existingInstance ?? (T)Activator.CreateInstance(SchemaType);
                transactionItem.NewInstance = instance ?? (T)Activator.CreateInstance(SchemaType);
                transactionItem.BlockAddresses = new LinkedList<long>(addresses);
                transactionItem.Storage = this;
                transaction.AddTransactionItem(transactionItem);

                var transactionLocation = this.YawnSite.SaveRecord(transaction as YawnSchema);
                if (transactionLocation == null)
                {
                    return null;
                }

                //this.PerfCounters.RecordWriteCounter.Increment();
                return location as StorageLocation;
            }
            else
            {
                Cache.Set(blocksToWrite[0].Address.ToString(), instance, new CacheItemPolicy());
                UpdateIndeciesForInstance(existingInstance, instance, location as StorageLocation);

                // Get existing adress for deletion
                if (existingLocation != null)
                {
                    Cache.Remove(existingLocation.Address.ToString());
                    StorageSyncLockCounter writeLock;
                    lock (writeLock = this.RecordLocker.LockRecord(existingLocation.Address, this.ResizeLock))
                    {
                        using (var unlocker = new StorageUnlocker(writeLock, this.RecordLocker))
                        {
                            existingAddresses.Select(x => FreeBlock(x));
                        }
                    }
                }

                this.PerfCounters.RecordWriteCounter.Increment();
                return location as StorageLocation;
            }
        }

        public IStorageLocation SaveRecord(YawnSchema inputInstance)
        {
            return this.SaveRecord(inputInstance, null);
        }

        public bool CommitSave(ITransactionItem transactionItem)
        {
            // Commit the blocks
            var item = transactionItem as BlockTransactionItem;
            using (var unlocker = new StorageUnlocker(this.RecordLocker.LockRecord(item.BlockAddresses.First(), this.ResizeLock), this.RecordLocker))
            {
                foreach (var address in item.BlockAddresses)
                {
                    using (var viewWriter = this.MappedFile.CreateViewAccessor(address, this.BlockSize))
                    {
                        BlockHeader header;
                        viewWriter.Read<BlockHeader>(0, out header);
                        var headerSize = BlockHelpers.GetHeaderSize();
                        header.BlockProperties |= BlockProperties.IsCommited;
                        viewWriter.Write<BlockHeader>(0, ref header);
                        viewWriter.Flush();
                        viewWriter.Dispose();
                    }
                }
            }

            var location = new BlockStorageLocation() { Address = item.BlockAddresses.First() };
            Cache.Set(item.BlockAddresses.First().ToString(), item.NewInstance, new CacheItemPolicy());

            // Get existing adress for deletion
            if (item.OldInstance != null)
            {
                bool commitOk;
                var keyIndex = this.Indicies["YawnKeyIndex"];
                var existingLocation = keyIndex.GetLocationForInstance(item.OldInstance) as BlockStorageLocation;
                if (existingLocation != null)
                {
                    Cache.Remove(existingLocation.Address.ToString());
                    this.RecordLocker.WaitForRecord(existingLocation.Address, 1);
                    StorageSyncLockCounter lck;
                    lock (lck = this.RecordLocker.LockRecord(existingLocation.Address, this.ResizeLock))
                    {
                        using (var unlocker = new StorageUnlocker(lck, this.RecordLocker))
                        {
                            List<long> existingAddress = GetRecordBlockAddresses(existingLocation);
                            commitOk = existingAddress.Select(x => FreeBlock(x)).Any(x => x == false);
                        }
                    }

                    if (!commitOk)
                    {
                        return false;
                    }
                }
            }

            UpdateIndeciesForInstance(item.OldInstance, item.NewInstance, location as StorageLocation);
            this.PerfCounters.RecordWriteCounter.Increment();
            return true;
        }

        public bool RollbackSave(ITransactionItem transactionItem)
        {
            // Rollback the new blocks
            var item = transactionItem as BlockTransactionItem;
            bool rolledBackOk;

            StorageSyncLockCounter lck;
            lock (lck = this.RecordLocker.LockRecord(item.BlockAddresses.First(), this.ResizeLock))
            {
                using (var unlocker = new StorageUnlocker(lck, this.RecordLocker))
                {
                    rolledBackOk = item.BlockAddresses.Select(x => FreeBlock(x)).Any(x => x == false);
                }
            }

            if (item.OriginalAddresses.Count > 0)
            {
                using (var unlocker = new StorageUnlocker(this.RecordLocker.LockRecord(item.OriginalAddresses.First(), this.ResizeLock), this.RecordLocker))
                {
                    foreach (var blockLocation in item.OriginalAddresses)
                    {
                        using (var viewWriter = this.MappedFile.CreateViewAccessor(blockLocation, this.BlockSize))
                        {
                            BlockHeader header;
                            viewWriter.Read<BlockHeader>(0, out header);
                            var headerSize = BlockHelpers.GetHeaderSize();
                            // commit the block
                            header.BlockProperties = (byte)(header.BlockProperties & BlockProperties.IsCommited);
                            // Un-Free the block
                            header.BlockProperties = (byte)(header.BlockProperties & BlockProperties.InUse);
                            viewWriter.Write<BlockHeader>(0, ref header);
                            viewWriter.Flush();
                            viewWriter.Dispose();
                        }
                    }
                }

                Cache.Set(item.OriginalAddresses.First().ToString(), item.OldInstance, new CacheItemPolicy());
                this.UpdateIndeciesForInstance(item.NewInstance, item.OldInstance, new BlockStorageLocation() { Address = item.OriginalAddresses.First() });
            }

            return true;
        }

        private bool FreeBlock(long blockLocation)
        {
            using (var unlocker = new StorageUnlocker(this.RecordLocker.LockRecord(blockLocation, this.ResizeLock), this.RecordLocker))
            {
                using (var viewWriter = this.MappedFile.CreateViewAccessor(blockLocation, this.BlockSize))
                {
                    BlockHeader header;
                    viewWriter.Read<BlockHeader>(0, out header);
                    var headerSize = BlockHelpers.GetHeaderSize();
                    // Un-commit the block
                    header.BlockProperties = (byte)(header.BlockProperties & ~BlockProperties.IsCommited);
                    // Free the block
                    header.BlockProperties = (byte)(header.BlockProperties & ~BlockProperties.InUse);
                    viewWriter.Write<BlockHeader>(0, ref header);
                    viewWriter.Flush();
                    viewWriter.Dispose();
                }
            }

            return true;
        }

        private List<long> GetRecordBlockAddresses(BlockStorageLocation blockLocation)
        {
            if(blockLocation == null)
            {
                return new List<long>();
            }

            long location = blockLocation.Address;
            bool lastBlock = false;
            List<long> addresses = new List<long>() { location };
            using (var unlocker = new StorageUnlocker(this.RecordLocker.LockRecord(blockLocation.Address, this.ResizeLock), this.RecordLocker))
            {
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
                while (!FreeBlocks.PopFreeBlock(out address))
                {
                    ResizeFile(this.NumberOfBufferBlocks);
                }

                addresses.Add(address);
            }

            return addresses.ToArray();
        }

        public void ResizeFile(int nuberOfBlocks)
        {
            if(this.WaitForResizer())
            {
                // If someone was already resizing the storage then there is no need to redo the resize
                return;
            }

            // Allreaders must be closed, the Memory Mapped file must be closed
            // therfore we must be mutualy exclusive form reads
            lock (this.ResizeLock)
            {
                this.RecordLocker.WaitForAllReaders();
                using (var unlocker = new BlockStorageUnlocker<T>(this))
                {
                    var firstAddressInNewArea = this.Capacity;
                    this.Capacity += this.NumberOfBufferBlocks * this.BlockSize;

                    // Close the mapped file an reopen with add capacity
                    this.MappedFile.Dispose();
                    this.MappedFile = MemoryMappedFile.CreateFromFile(this.FilePath, FileMode.OpenOrCreate, this.TypeNameNormilized, this.Capacity, MemoryMappedFileAccess.ReadWrite);

                    FreeBlocks.AddFreeBlockRange(firstAddressInNewArea, this.NumberOfBufferBlocks, this.BlockSize);

                    this.PerfCounters.ResizeCounter.Increment();
                }
            }
        }

        private void UpdateIndeciesForInstance(YawnSchema oldRecord, YawnSchema newRecord, StorageLocation newLocation)
        {
            this.PerfCounters.IndexingCounter.Increment();
            foreach (var index in this.Indicies)
            {
                index.Value.UpdateIndex(oldRecord, newRecord, newLocation);
            }
        }

        private void DeleteIndeciesForInstance(YawnSchema instance)
        {
            this.PerfCounters.IndexingCounter.Increment();
            foreach (var index in this.Indicies)
            {
                index.Value.DeleteIndex(instance);
            }
        }

        public T ReadRecord(IStorageLocation fromLocation)
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
                this.PerfCounters.RecordReadFromCacheCounter.Increment();
                return Cloner.Clone<T>(cacheInstance as T);
            }

            bool lastBlock = false;
            byte[] buffer = null;
            int bytesReadSofar = 0;
            using (var unlocker = new StorageUnlocker(this.RecordLocker.LockRecord(FirstLocation, this.ResizeLock), this.RecordLocker))
            {
                for (int i = 0; !lastBlock; i++)
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
                }
            }

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

                yield return this.ReadRecord(location) as TE;
            }

            yield break;
        }

        public IEnumerable<TE> GetAllRecords<TE>() where TE : YawnSchema
        {
            return GetRecords<TE>(Indicies["YawnKeyIndex"].EnumerateAllLocations().ToArray());
        }

        public YawnSchema CreateRecord()
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

        public bool DeleteRecord(YawnSchema instance, ITransaction transaction)
        {
            if (!this.YawnSite.TransactionsEnabled)
            {
                throw new DatabaseTransactionsAreDisabled();
            }

            var keyIndex = this.Indicies["YawnKeyIndex"];
            var existingLocation = keyIndex.GetLocationForInstance(instance) as BlockStorageLocation;
            var existingAddresses = GetRecordBlockAddresses(existingLocation);
            var transactionItem = new BlockTransactionItem();
            transactionItem.SchemaType = SchemaType.AssemblyQualifiedName;
            transactionItem.OriginalAddresses = new LinkedList<long>(existingAddresses);
            transactionItem.OldInstance = instance ?? (T)Activator.CreateInstance(SchemaType);
            transactionItem.NewInstance =  (T)Activator.CreateInstance(SchemaType);
            transactionItem.ItemAction = Transactions.TransactionAction.Delete;
            transactionItem.Storage = this;
            transaction.AddTransactionItem(transactionItem);

            return this.YawnSite.SaveRecord(transaction as YawnSchema) == null ? false : true;
        }

        public bool DeleteRecord(YawnSchema instance)
        {
            long firstLocation = this.GetExistingAddress(instance);

            if(firstLocation == -1 )
            {
                return true;
            }

            long location = firstLocation;
            bool lastBlock = false;
            var blankHeader = new BlockHeader();
            StorageSyncLockCounter lck;
            lock (lck = this.RecordLocker.LockRecord(location, this.ResizeLock))
            {
                using (var unlocker = new StorageUnlocker(lck, this.RecordLocker))
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
            }

            this.DeleteIndeciesForInstance(instance);
            this.Cache.Remove(firstLocation.ToString());
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
                            T record = this.ReadRecord(location);
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

        public bool CommitTransactionItem(ITransactionItem transactionItem)
        {
            var item = transactionItem as BlockTransactionItem;
            switch (item.ItemAction)
            {
                case Transactions.TransactionAction.Delete:
                    return this.DeleteRecord(item.OldInstance);

                case Transactions.TransactionAction.Update:
                case Transactions.TransactionAction.Insert:
                    return this.CommitSave(transactionItem);

                default:
                    return false;
            }
        }

        public bool RollbackTransactionItem(ITransactionItem transactionItem)
        {
            var item = transactionItem as BlockTransactionItem;
            switch (item.ItemAction)
            {
                case Transactions.TransactionAction.Update:
                case Transactions.TransactionAction.Insert:
                    return this.RollbackSave(transactionItem);
                
                // since delete did nothing on disk simply ignore
                case Transactions.TransactionAction.Delete:
                default:
                    return false;
            }
        }

        private long GetExistingAddress(YawnSchema instance)
        {
            var keyIndex = this.Indicies["YawnKeyIndex"];
            var existingLocation = keyIndex.GetLocationForInstance(instance) as BlockStorageLocation;
            if (existingLocation != null)
            {
                return existingLocation.Address;
            }

            return -1;
        }

        internal void AddResizer()
        {
            lock(this.ResizeLock)
            {
                Interlocked.Increment(ref this.Resizers);
            }
        }

        internal void RemoveResizer()
        {
            Interlocked.Decrement(ref this.Resizers);
        }

        private bool WaitForResizer()
        {
            bool someoneWasAlreadyResizing = false;
            while(this.Resizers>0)
            {
                someoneWasAlreadyResizing = true;
                Thread.Sleep(0);
            }

            return someoneWasAlreadyResizing;
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
