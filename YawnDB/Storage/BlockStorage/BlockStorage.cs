// <copyright file="BlockStorage.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

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
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;
    using Bond;
    using Bond.IO.Unsafe;
    using Bond.Protocols;
    using YawnDB.EventSources;
    using YawnDB.Exceptions;
    using YawnDB.Interfaces;
    using YawnDB.PerformanceCounters;
    using YawnDB.Utils;

    public class BlockStorage<T> : IStorage, IDisposable where T : YawnSchema
    {
        private IYawn yawnSite;

        public StorageState State { get; private set; } = StorageState.Closed;

        public IDictionary<string, IIndex> Indicies { get; private set; } = new Dictionary<string, IIndex>();

        public Type SchemaType { get; } = typeof(T);

        public int BlockSize { get; }

        public long Capacity { get; private set; }

        public int NumberOfBufferBlocks { get; set; }

        public string FilePath { get; private set; }

        public string FullStorageName { get; }

        private MemoryCache cache;

        private string typeNameNormilized;

        private string pathToSchemaFolder;

        private MemoryMappedFile mappedFile;

        private FreeBlocks freeBlocks;

        private Deserializer<CompactBinaryReader<InputBuffer>> schemaDeserializer = new Deserializer<CompactBinaryReader<InputBuffer>>(typeof(T));

        private Serializer<CompactBinaryWriter<OutputBuffer>> schemaSerializer = new Serializer<CompactBinaryWriter<OutputBuffer>>(typeof(T));

        private Cloner<T> cloner = new Cloner<T>(typeof(T));

        private object resizeLock = new object();

        private int resizers = 0;

        private object autoIdLock = new object();

        private long nextIndex = 0;

        private StorageCounters perfCounters;

        private StorageLocker recordLocker;

        private List<PropertyInfo> referencingProperties = new List<PropertyInfo>();

        public BlockStorage(IYawn yawnSite, int blockSize = 512, int numberOfBufferBlocks = 10000)
        {
            this.yawnSite = yawnSite;
            this.BlockSize = blockSize;
            this.NumberOfBufferBlocks = numberOfBufferBlocks;

            this.typeNameNormilized = this.SchemaType.Namespace + "." + this.SchemaType.Name;
            if (this.SchemaType.IsGenericType)
            {
                this.typeNameNormilized += "[";
                foreach (var genericArgument in this.SchemaType.GetGenericArguments())
                {
                    this.typeNameNormilized += genericArgument.Namespace + "." + genericArgument.Name;
                }

                this.typeNameNormilized += "]";
            }

            this.FullStorageName = yawnSite.DatabaseName + "_" + this.typeNameNormilized;
            this.cache = new MemoryCache(this.FullStorageName);
            this.pathToSchemaFolder = Path.Combine(this.yawnSite.DefaultStoragePath, this.typeNameNormilized);
            if (!Directory.Exists(this.pathToSchemaFolder))
            {
                Directory.CreateDirectory(this.pathToSchemaFolder);
            }

            this.FilePath = Path.Combine(this.pathToSchemaFolder, this.typeNameNormilized + ".ydb");
        }

        public void Open()
        {
            this.perfCounters = new StorageCounters(this.FullStorageName);
            this.recordLocker = new StorageLocker(this.perfCounters.WriteContentionCounter);
            StorageEventSource.Log.InitializeStart("BlockStorage: " + this.FullStorageName);

            bool fileExist = File.Exists(this.FilePath);

            var minBlockSize = BlockHelpers.GetHeaderSize() + 1;
            if (this.BlockSize < minBlockSize)
            {
                throw new InvalidDataException("BlockSize cannot be lower than " + minBlockSize + " bytes");
            }

            if (fileExist)
            {
                this.Capacity = new FileInfo(this.FilePath).Length;
                if ((this.Capacity % this.BlockSize) != 0)
                {
                    throw new DataMisalignedException("File size in bytes for " + this.FilePath + " is missaligned for block size " + this.BlockSize);
                }
            }
            else
            {
                this.Capacity = this.NumberOfBufferBlocks * this.BlockSize;
            }

            this.mappedFile = MemoryMappedFile.CreateFromFile(this.FilePath, FileMode.OpenOrCreate, this.typeNameNormilized, this.Capacity, MemoryMappedFileAccess.ReadWrite);

            var freeBlocksFile = Path.Combine(this.pathToSchemaFolder, "Freeblocks.bin");
            this.freeBlocks = new FreeBlocks(freeBlocksFile);
            if (!fileExist)
            {
                this.freeBlocks.AddFreeBlockRange(0, this.NumberOfBufferBlocks, this.BlockSize);
            }
            else if (!File.Exists(freeBlocksFile))
            {
                using (var mapAccessor = this.mappedFile.CreateViewAccessor(0, this.Capacity, MemoryMappedFileAccess.Read))
                {
                    this.freeBlocks.ScanFreeBlocksFromMap(mapAccessor, this.Capacity, this.BlockSize);
                    mapAccessor.Flush();
                }
            }

            this.Indicies = Utilities.GetIndeciesFromSchema(this.SchemaType, typeof(BlockStorageLocation));
            List<IIndex> needReindexing = new List<IIndex>();

            foreach (var index in this.Indicies)
            {
                if (!index.Value.Initialize(this.pathToSchemaFolder, true))
                {
                    needReindexing.Add(index.Value);
                }
            }

            this.State = StorageState.Open;
            this.ReIndexStorage(needReindexing);

            var proterties = typeof(T).GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in proterties)
            {
                if (typeof(IReference).IsAssignableFrom(prop.PropertyType))
                {
                    this.referencingProperties.Add(prop);
                }
            }

            StorageEventSource.Log.InitializeFinish(this.FullStorageName);
            this.perfCounters.InitializeCounter.Increment();
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
                throw new DatabaseIsClosedException($"An attemp was made to write to database '{this.yawnSite.DatabaseName}' which is closed");
            }

            if (!this.yawnSite.TransactionsEnabled && transaction != null)
            {
                throw new DatabaseTransactionsAreDisabled();
            }

            StorageEventSource.Log.RecordWriteStart(this.FullStorageName, inputInstance.Id);
            this.perfCounters.RecordWriteStartCounter.Increment();
            var isTransaction = transaction != null;
            var instance = this.cloner.Clone<T>(inputInstance as T);
            var output = new OutputBuffer();
            var writer = new CompactBinaryWriter<OutputBuffer>(output);
            this.schemaSerializer.Serialize(instance, writer);
            int recordSize = output.Data.Count;
            int realBlockSize = this.BlockSize - BlockHelpers.GetHeaderSize();
            var numberOfBlocksNeeded = recordSize / realBlockSize;
            numberOfBlocksNeeded += (recordSize % realBlockSize) == 0 ? 0 : 1;
            long[] addresses = this.GetFreeBlocks(numberOfBlocksNeeded);

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
                        RecordSize = recordSize
                    },
                    Address = addresses[i],
                    BlockBytes = output.Data.Array.Skip(i * realBlockSize).Take(realBlockSize).ToArray()
                });
            }

            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(addresses[0], this.resizeLock), this.recordLocker))
            {
                // Write the prepared blocks
                foreach (var blk in blocksToWrite)
                {
                    this.WriteBlock(blk);
                }
            }

            var keyIndex = this.Indicies["YawnKeyIndex"];
            var existingLocation = keyIndex.GetLocationForInstance(instance) as BlockStorageLocation;
            T existingInstance = this.ReadRecord(existingLocation);
            List<long> existingAddresses = this.GetRecordBlockAddresses(existingLocation);

            if (isTransaction)
            {
                var transactionItem = new BlockTransactionItem();
                transactionItem.ItemAction = Transactions.TransactionAction.Update;
                transactionItem.SchemaType = this.SchemaType.AssemblyQualifiedName;
                transactionItem.OriginalAddresses = new LinkedList<long>(existingAddresses);
                transactionItem.OldInstance = existingInstance ?? (T)Activator.CreateInstance(this.SchemaType);
                transactionItem.NewInstance = instance ?? (T)Activator.CreateInstance(this.SchemaType);
                transactionItem.BlockAddresses = new LinkedList<long>(addresses);
                transactionItem.Storage = this;
                transaction.AddTransactionItem(transactionItem);

                var transactionLocation = this.yawnSite.SaveRecord(transaction as YawnSchema);
                if (transactionLocation == null)
                {
                    return null;
                }

                return location as StorageLocation;
            }
            else
            {
                this.cache.Set(blocksToWrite[0].Address.ToString(), instance, new CacheItemPolicy());
                this.UpdateIndeciesForInstance(existingInstance, instance, location as StorageLocation);

                // Get existing adress for deletion
                if (existingLocation != null)
                {
                    this.cache.Remove(existingLocation.Address.ToString());
                    StorageSyncLockCounter writeLock;
                    lock (writeLock = this.recordLocker.LockRecord(existingLocation.Address, this.resizeLock))
                    {
                        using (var unlocker = new StorageUnlocker(writeLock, this.recordLocker))
                        {
                            existingAddresses.Select(x => this.FreeBlock(x));
                        }
                    }
                }

                StorageEventSource.Log.RecordWriteFinish(this.FullStorageName, instance.Id);
                this.perfCounters.RecordWriteFinishedCounter.Increment();
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
            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(item.BlockAddresses.First(), this.resizeLock), this.recordLocker))
            {
                foreach (var address in item.BlockAddresses)
                {
                    using (var viewWriter = this.mappedFile.CreateViewAccessor(address, this.BlockSize))
                    {
                        BlockHeader header;
                        viewWriter.Read<BlockHeader>(0, out header);
                        var headerSize = BlockHelpers.GetHeaderSize();
                        header.BlockProperties |= BlockProperties.IsCommited;
                        viewWriter.Write<BlockHeader>(0, ref header);
                        viewWriter.Flush();
                    }
                }
            }

            var location = new BlockStorageLocation() { Address = item.BlockAddresses.First() };
            this.cache.Set(item.BlockAddresses.First().ToString(), item.NewInstance, new CacheItemPolicy());

            // Get existing adress for deletion
            if (item.OldInstance != null)
            {
                bool commitOk;
                var keyIndex = this.Indicies["YawnKeyIndex"];
                var existingLocation = keyIndex.GetLocationForInstance(item.OldInstance) as BlockStorageLocation;
                if (existingLocation != null)
                {
                    this.cache.Remove(existingLocation.Address.ToString());
                    this.recordLocker.WaitForRecord(existingLocation.Address, 1);
                    StorageSyncLockCounter lck;
                    lock (lck = this.recordLocker.LockRecord(existingLocation.Address, this.resizeLock))
                    {
                        using (var unlocker = new StorageUnlocker(lck, this.recordLocker))
                        {
                            List<long> existingAddress = this.GetRecordBlockAddresses(existingLocation);
                            commitOk = existingAddress.Select(x => this.FreeBlock(x)).Any(x => x == false);
                        }
                    }

                    if (!commitOk)
                    {
                        return false;
                    }
                }
            }

            this.UpdateIndeciesForInstance(item.OldInstance, item.NewInstance, location as StorageLocation);
            StorageEventSource.Log.RecordWriteFinish(this.FullStorageName, item.NewInstance.Id);
            return true;
        }

        public bool RollbackSave(ITransactionItem transactionItem)
        {
            // Rollback the new blocks
            var item = transactionItem as BlockTransactionItem;
            bool rolledBackOk;

            StorageSyncLockCounter lck;
            lock (lck = this.recordLocker.LockRecord(item.BlockAddresses.First(), this.resizeLock))
            {
                using (var unlocker = new StorageUnlocker(lck, this.recordLocker))
                {
                    rolledBackOk = item.BlockAddresses.Select(x => this.FreeBlock(x)).Any(x => x == false);
                }
            }

            if (item.OriginalAddresses.Count > 0)
            {
                using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(item.OriginalAddresses.First(), this.resizeLock), this.recordLocker))
                {
                    foreach (var blockLocation in item.OriginalAddresses)
                    {
                        using (var viewWriter = this.mappedFile.CreateViewAccessor(blockLocation, this.BlockSize))
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
                        }
                    }
                }

                this.cache.Set(item.OriginalAddresses.First().ToString(), item.OldInstance, new CacheItemPolicy());
                this.UpdateIndeciesForInstance(item.NewInstance, item.OldInstance, new BlockStorageLocation() { Address = item.OriginalAddresses.First() });
            }

            return true;
        }

        private bool FreeBlock(long blockLocation)
        {
            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(blockLocation, this.resizeLock), this.recordLocker))
            {
                using (var viewWriter = this.mappedFile.CreateViewAccessor(blockLocation, this.BlockSize))
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
                }
            }

            return true;
        }

        private List<long> GetRecordBlockAddresses(BlockStorageLocation blockLocation)
        {
            if (blockLocation == null)
            {
                return new List<long>();
            }

            long location = blockLocation.Address;
            bool lastBlock = false;
            List<long> addresses = new List<long>() { location };
            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(blockLocation.Address, this.resizeLock), this.recordLocker))
            {
                while (!lastBlock)
                {
                    using (var readerView = this.mappedFile.CreateViewAccessor(location, this.BlockSize))
                    {
                        BlockHeader header;
                        readerView.Read<BlockHeader>(0, out header);
                        lastBlock = (header.BlockProperties & BlockProperties.IsLastBlockInRecord) != 0;
                        if (!lastBlock)
                        {
                            addresses.Add(header.NextBlockLocation);
                        }
                    }
                }
            }

            return addresses;
        }

        private bool WriteBlock(Block block)
        {
            using (var viewWriter = this.mappedFile.CreateViewAccessor(block.Address, this.BlockSize, MemoryMappedFileAccess.ReadWrite))
            {
                var headerSize = BlockHelpers.GetHeaderSize();
                viewWriter.Write<BlockHeader>(0, ref block.Header);
                viewWriter.WriteArray(headerSize, block.BlockBytes, 0, this.BlockSize - headerSize);
                viewWriter.Flush();
            }

            return true;
        }

        private long[] GetFreeBlocks(int numberOfBlocks)
        {
            long address;
            List<long> addresses = new List<long>();

            for (int i = 0; i < numberOfBlocks; i++)
            {
                while (!this.freeBlocks.PopFreeBlock(out address))
                {
                    this.ResizeFile(this.NumberOfBufferBlocks);
                }

                addresses.Add(address);
            }

            return addresses.ToArray();
        }

        public void ResizeFile(int nuberOfBlocks)
        {
            if (this.WaitForResizer())
            {
                // If someone was already resizing the storage then there is no need to redo the resize
                return;
            }

            // Allreaders must be closed in order to close Memory Mapped file
            // Therefore we must be mutualy exclusive form all reads and writes
            lock (this.resizeLock) // <--this lock says no new friends (no new readers/writers)
            {
                this.recordLocker.WaitForAllReaders();

                // TODO: GARBAGE COLLECT FREE BLOCKS HERE
                using (var unlocker = new BlockStorageUnlocker<T>(this))
                {
                    var firstAddressInNewArea = this.Capacity;
                    this.Capacity += this.NumberOfBufferBlocks * this.BlockSize;

                    // Close the mapped file and reopen with added capacity
                    this.mappedFile.Dispose();
                    this.mappedFile = MemoryMappedFile.CreateFromFile(this.FilePath, FileMode.OpenOrCreate, this.typeNameNormilized, this.Capacity, MemoryMappedFileAccess.ReadWrite);

                    this.freeBlocks.AddFreeBlockRange(firstAddressInNewArea, this.NumberOfBufferBlocks, this.BlockSize);

                    this.perfCounters.ResizeCounter.Increment();
                }
            }
        }

        private void UpdateIndeciesForInstance(YawnSchema oldRecord, YawnSchema newRecord, StorageLocation newLocation)
        {
            StorageEventSource.Log.IndexingStart(this.FullStorageName, newRecord.Id);
            this.perfCounters.IndexingStartCounter.Increment();
            foreach (var index in this.Indicies)
            {
                index.Value.UpdateIndex(oldRecord, newRecord, newLocation);
            }

            StorageEventSource.Log.IndexingFinish(this.FullStorageName, newRecord.Id);
            this.perfCounters.IndexingFinishedCounter.Increment();
        }

        private void DeleteIndeciesForInstance(YawnSchema instance)
        {
            StorageEventSource.Log.IndexingStart(this.FullStorageName + ": Delete", instance.Id);
            this.perfCounters.IndexingStartCounter.Increment();
            foreach (var index in this.Indicies)
            {
                index.Value.DeleteIndex(instance);
            }

            StorageEventSource.Log.IndexingFinish(this.FullStorageName + ": Delete", instance.Id);
            this.perfCounters.IndexingFinishedCounter.Increment();
        }

        public T ReadRecord(IStorageLocation fromLocation)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to read from database '{this.yawnSite.DatabaseName}' which is closed");
            }

            BlockStorageLocation blockStorageLocation = fromLocation as BlockStorageLocation;
            if (blockStorageLocation == null)
            {
                return null;
            }

            long location = blockStorageLocation.Address;
            long firstLocation = location;
            var cacheInstance = this.cache.Get(location.ToString());
            if (cacheInstance != null)
            {
                StorageEventSource.Log.RecordReadFromCahe(this.FullStorageName, (cacheInstance as T).Id);
                this.perfCounters.RecordReadFromCacheCounter.Increment();
                return this.cloner.Clone<T>(cacheInstance as T);
            }

            StorageEventSource.Log.RecordReadStart(this.FullStorageName, blockStorageLocation.Address);
            this.perfCounters.RecordReadStartCounter.Increment();

            bool lastBlock = false;
            byte[] buffer = null;
            int bytesReadSofar = 0;
            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(firstLocation, this.resizeLock), this.recordLocker))
            {
                for (int i = 0; !lastBlock; i++)
                {
                    using (var readerView = this.mappedFile.CreateViewAccessor(location, this.BlockSize))
                    {
                        BlockHeader header;
                        readerView.Read<BlockHeader>(0, out header);
                        lastBlock = (header.BlockProperties & BlockProperties.IsLastBlockInRecord) != 0;
                        location = header.NextBlockLocation;
                        if (i == 0)
                        {
                            buffer = new byte[header.RecordSize];
                        }

                        var bytesInBlock = this.BytesOnBlock(header.RecordSize, lastBlock);
                        readerView.ReadArray(BlockHelpers.GetHeaderSize(), buffer, bytesReadSofar, bytesInBlock);
                        bytesReadSofar += bytesInBlock;
                    }
                }
            }

            var input = new InputBuffer(buffer.ToArray());
            var reader = new CompactBinaryReader<InputBuffer>(input);
            T instance = this.schemaDeserializer.Deserialize<T>(reader);
            this.cache.Set(location.ToString(), instance, new CacheItemPolicy());
            StorageEventSource.Log.RecordSerializeFinish(this.FullStorageName, blockStorageLocation.Address);
            this.perfCounters.RecordWriteFinishedCounter.Increment();
            return this.PropagateSite(this.cloner.Clone<T>(instance));
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
            return this.GetRecords<TE>(this.Indicies["YawnKeyIndex"].EnumerateAllLocations().ToArray());
        }

        public YawnSchema CreateRecord()
        {
            var record = Activator.CreateInstance(typeof(T)) as YawnSchema;
            record.Id = this.GetNextID();
            return this.PropagateSite(record as T);
        }

        public long GetNextID()
        {
            lock (this.autoIdLock)
            {
                Interlocked.Increment(ref this.nextIndex);
            }

            return this.nextIndex;
        }

        public bool DeleteRecord(YawnSchema instance, ITransaction transaction)
        {
            if (!this.yawnSite.TransactionsEnabled)
            {
                throw new DatabaseTransactionsAreDisabled();
            }

            var keyIndex = this.Indicies["YawnKeyIndex"];
            var existingLocation = keyIndex.GetLocationForInstance(instance) as BlockStorageLocation;
            var existingAddresses = this.GetRecordBlockAddresses(existingLocation);
            var transactionItem = new BlockTransactionItem();
            transactionItem.SchemaType = this.SchemaType.AssemblyQualifiedName;
            transactionItem.OriginalAddresses = new LinkedList<long>(existingAddresses);
            transactionItem.OldInstance = instance ?? (T)Activator.CreateInstance(this.SchemaType);
            transactionItem.NewInstance = instance ?? (T)Activator.CreateInstance(this.SchemaType);
            transactionItem.ItemAction = Transactions.TransactionAction.Delete;
            transactionItem.Storage = this;
            transaction.AddTransactionItem(transactionItem);

            return this.yawnSite.SaveRecord(transaction as YawnSchema) == null ? false : true;
        }

        public bool DeleteRecord(YawnSchema instance)
        {
            StorageEventSource.Log.RecordDeleteStart(this.FullStorageName, instance.Id);
            this.perfCounters.RecordDeleteStartCounter.Increment();
            long firstLocation = this.GetExistingAddress(instance);
            if (firstLocation == -1)
            {
                return true;
            }

            long location = firstLocation;
            bool lastBlock = false;
            var blankHeader = default(BlockHeader);
            StorageSyncLockCounter lck;
            lock (lck = this.recordLocker.LockRecord(location, this.resizeLock))
            {
                using (var unlocker = new StorageUnlocker(lck, this.recordLocker))
                {
                    while (!lastBlock)
                    {
                        using (var deleteView = this.mappedFile.CreateViewAccessor(location, this.BlockSize))
                        {
                            BlockHeader header;
                            deleteView.Read<BlockHeader>(0, out header);
                            lastBlock = (header.BlockProperties & BlockProperties.IsLastBlockInRecord) != 0;
                            deleteView.Write<BlockHeader>(0, ref blankHeader);

                            this.freeBlocks.AddFreeBlock(location);
                            location = header.NextBlockLocation;
                        }
                    }
                }
            }

            this.DeleteIndeciesForInstance(instance);
            this.cache.Remove(firstLocation.ToString());
            StorageEventSource.Log.RecordDeleteFinish(this.FullStorageName, instance.Id);
            this.perfCounters.RecordDeleteFinishedCounter.Increment();
            return true;
        }

        public void ReIndexStorage(IList<IIndex> needReindexing)
        {
            if (!needReindexing.Any())
            {
                return;
            }

            lock (this.resizeLock)
            {
                using (var mapAccessor = this.mappedFile.CreateViewAccessor())
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
                            if (this.nextIndex < record.Id)
                            {
                                this.nextIndex = record.Id;
                            }

                            foreach (var index in needReindexing)
                            {
                                index.SetIndex(record, location as StorageLocation);
                            }
                        }
                    }
                }
            }
        }

        public void Close()
        {
            lock (this.resizeLock)
            {
                this.recordLocker.WaitForAllReaders();

                foreach (var index in this.Indicies)
                {
                    index.Value.Close(false);
                }

                this.freeBlocks.SaveToFile();
                this.mappedFile.Dispose();
                this.State = StorageState.Closed;
            }
        }

        private T PropagateSite(T instance)
        {
            foreach (var prop in this.referencingProperties)
            {
                ((IReference)prop.GetValue(instance)).YawnSite = this.yawnSite;
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
            lock (this.resizeLock)
            {
                Interlocked.Increment(ref this.resizers);
            }
        }

        internal void RemoveResizer()
        {
            Interlocked.Decrement(ref this.resizers);
        }

        private bool WaitForResizer()
        {
            bool someoneWasAlreadyResizing = false;
            while (this.resizers > 0)
            {
                someoneWasAlreadyResizing = true;

                // Thread.Sleep(0);
                Thread.Yield();
            }

            return someoneWasAlreadyResizing;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    this.cache.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                this.disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BlockStorage() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);

            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
