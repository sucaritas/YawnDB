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
    ║ │ NextBlockLocation │ 8 Bytes (1 long int) │  ╞══ 25 bytes    ║  │
    ║ ├───────────────────┼──────────────────────┤  │   Header size ║  │
    ║ │ RecordSize        │ 8 Bytes (1 long int) │  │               ║  │
    ║ ├───────────────────┼──────────────────────┤  │               ║  │
    ║ │ RecordId          │ 8 Bytes (1 long int) │  │               ║  ╞══ Block Size
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
    using YawnDB.Index;
    using YawnDB.PerformanceCounters;
    using YawnDB.Storage;
    using YawnDB.Transactions;
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
            this.referencingProperties.Clear();
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

        public StorageLocation InsertRecord(YawnSchema instanceToInsert)
        {
            return this.SaveRecord(instanceToInsert);
        }

        public StorageLocation InsertRecord(YawnSchema instanceToInsert, ITransaction transaction)
        {
            return this.SaveRecord(instanceToInsert, transaction);
        }

        public StorageLocation SaveRecord(YawnSchema inputInstance, ITransaction transaction)
        {
            if (this.State == StorageState.Closed)
            {
                throw new DatabaseIsClosedException($"An attemp was made to write to database '{this.yawnSite.DatabaseName}' which is closed");
            }

            if (!this.yawnSite.TransactionsEnabled && transaction != null)
            {
                throw new DatabaseTransactionsAreDisabled();
            }

            var lck = this.GetRecordLockName(this.SchemaType) + "_" + inputInstance.Id;
            this.yawnSite.RecordLocker.WaitForRecordLock(lck, Locking.RecordLockType.Write);

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
            BlockStorageLocation location = new BlockStorageLocation() { Id = inputInstance.Id };
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
                        RecordSize = recordSize,
                        RecordId = instance.Id,
                    },
                    Address = addresses[i],
                    BlockBytes = output.Data.Array.Skip(i * realBlockSize).Take(realBlockSize).ToArray()
                });
            }

            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(instance.Id, this.resizeLock), this.recordLocker))
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
                if (existingInstance != null)
                {
                    transactionItem.OldInstance = new Bonded<T>(existingInstance);
                }
                else
                {
                    transactionItem.OldInstance = new Bonded<YawnSchema>(new YawnSchema() { Id = -1 });
                }

                transactionItem.NewInstance = new Bonded<T>(instance);
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
                this.UpdateIndeciesForInstance(existingInstance, instance, new Bonded<BlockStorageLocation>(location));
                if (existingInstance != null)
                {
                    // We dont remove the old instance from the cache as there may be some queries that are still pending to pull it
                    // We opt to redirect the old cahce location to the new instance and set it to expire in 5 min.
                    // 5 min. should be engough to to drain any query.
                    this.cache.Set(existingInstance?.Id.ToString(), instance, new CacheItemPolicy()
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.ToOffset(new TimeSpan(0, 5, 0))
                    });
                }

                this.cache.Set(instance.Id.ToString(), instance, new CacheItemPolicy());

                // Get existing adress for deletion
                if (existingLocation != null)
                {
                    StorageSyncLockCounter writeLock;
                    lock (writeLock = this.recordLocker.LockRecord(existingLocation.Id, this.resizeLock))
                    {
                        using (var unlocker = new StorageUnlocker(writeLock, this.recordLocker))
                        {
                            existingAddresses.Select(x => this.FreeBlock(x, existingLocation.Id));
                        }
                    }
                }

                StorageEventSource.Log.RecordWriteFinish(this.FullStorageName, instance.Id);
                this.perfCounters.RecordWriteFinishedCounter.Increment();
                return location as StorageLocation;
            }
        }

        public StorageLocation SaveRecord(YawnSchema inputInstance)
        {
            return this.SaveRecord(inputInstance, null);
        }

        public bool CommitSave(BlockTransactionItem transactionItem)
        {
            var item = transactionItem;
            var newInstance = item.NewInstance.Deserialize<T>();
            var oldYawnInstance = item.OldInstance.Deserialize();
            var oldInstance = item.OldInstance.Deserialize<T>();
            var lck = this.GetRecordLockName(this.SchemaType) + "_" + newInstance.Id;
            this.yawnSite.RecordLocker.WaitForRecordLock(lck, Locking.RecordLockType.Read);

            // Commit the blocks
            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(newInstance.Id, this.resizeLock), this.recordLocker))
            {
                foreach (var address in item.BlockAddresses)
                {
                    using (var mapAccessor = this.mappedFile.CreateViewAccessor(address, this.BlockSize))
                    {
                        BlockHeader header;
                        mapAccessor.Read<BlockHeader>(0, out header);
                        var headerSize = BlockHelpers.GetHeaderSize();
                        header.BlockProperties |= BlockProperties.IsCommited;
                        mapAccessor.Write<BlockHeader>(0, ref header);
                        mapAccessor.Flush();
                    }
                }
            }

            var location = new BlockStorageLocation() { Id = newInstance.Id, Address = item.BlockAddresses.First() };
            if (newInstance.Id != -1)
            {
                // We dont remove the old instance from the cache as there may be some queries that are still pending to pull it
                // We opt to redirect the old cahce location to the new instance and set it to expire in 5 min.
                // 5 min. should be engough to to drain any query.
                this.cache.Set(oldInstance.Id.ToString(), newInstance, new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.Now.ToOffset(new TimeSpan(0, 5, 0))
                });
            }

            this.cache.Set(newInstance.Id.ToString(), newInstance, new CacheItemPolicy());

            // Get existing adress for deletion
            if (oldYawnInstance.Id != -1)
            {
                bool commitOk;
                var keyIndex = this.Indicies["YawnKeyIndex"];
                var existingLocation = keyIndex.GetLocationForInstance(oldInstance) as BlockStorageLocation;
                if (existingLocation != null)
                {
                    StorageSyncLockCounter recordLock;
                    lock (recordLock = this.recordLocker.LockRecord(oldInstance.Id, this.resizeLock))
                    {
                        using (var unlocker = new StorageUnlocker(recordLock, this.recordLocker))
                        {
                            List<long> existingAddress = this.GetRecordBlockAddresses(existingLocation);
                            commitOk = existingAddress.Select(x => this.FreeBlock(x, oldInstance.Id)).Any(x => x == false);
                        }
                    }

                    if (!commitOk)
                    {
                        return false;
                    }
                }
            }

            this.UpdateIndeciesForInstance(oldInstance, newInstance, new Bonded<BlockStorageLocation>(location));
            StorageEventSource.Log.RecordWriteFinish(this.FullStorageName, newInstance.Id);
            return true;
        }

        public bool RollbackSave(BlockTransactionItem transactionItem)
        {
            // Rollback the new blocks
            var item = transactionItem;
            bool rolledBackOk;
            var newInstance = item.NewInstance.Deserialize<T>();
            var oldYawnInstance = item.OldInstance.Deserialize();
            var oldInstance = item.OldInstance.Deserialize<T>();

            this.cache.Remove(newInstance.Id.ToString());
            StorageSyncLockCounter lck;
            lock (lck = this.recordLocker.LockRecord(newInstance.Id, this.resizeLock))
            {
                using (var unlocker = new StorageUnlocker(lck, this.recordLocker))
                {
                    rolledBackOk = item.BlockAddresses.Select(x => this.FreeBlock(x, newInstance.Id)).Any(x => x == false);
                }
            }

            if (oldYawnInstance.Id != -1)
            {
                using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(oldInstance.Id, this.resizeLock), this.recordLocker))
                {
                    this.SaveRecord(oldInstance);
                }

                this.cache.Set(oldInstance.Id.ToString(), oldInstance, new CacheItemPolicy()
                {
                    AbsoluteExpiration = new DateTimeOffset(DateTimeOffset.Now.Ticks, new TimeSpan(0, 5, 0))
                });
                this.UpdateIndeciesForInstance(newInstance, oldInstance, new Bonded<BlockStorageLocation>(new BlockStorageLocation() { Id = oldInstance.Id, Address = item.OriginalAddresses.First() }));
            }

            return true;
        }

        private bool FreeBlock(long blockLocation, long id)
        {
            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(id, this.resizeLock), this.recordLocker))
            {
                using (var mapAccessor = this.mappedFile.CreateViewAccessor(blockLocation, this.BlockSize))
                {
                    // Make sure we are freing a block related to the record id. if not skip
                    BlockHeader header;
                    mapAccessor.Read<BlockHeader>(0, out header);
                    if (header.RecordId == id)
                    {
                        return false;
                    }

                    header = new BlockHeader() { BlockProperties = 0, NextBlockLocation = 0, RecordSize = 0, RecordId = 0, };
                    var headerSize = BlockHelpers.GetHeaderSize();
                    mapAccessor.Write<BlockHeader>(0, ref header);
                    mapAccessor.Flush();
                }
            }

            this.freeBlocks.AddFreeBlock(blockLocation);
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
            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(blockLocation.Id, this.resizeLock), this.recordLocker))
            {
                while (!lastBlock)
                {
                    using (var mapAccessor = this.mappedFile.CreateViewAccessor(location, this.BlockSize))
                    {
                        BlockHeader header;
                        mapAccessor.Read<BlockHeader>(0, out header);
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
            using (var mapAccessor = this.mappedFile.CreateViewAccessor(block.Address, this.BlockSize, MemoryMappedFileAccess.ReadWrite))
            {
                var headerSize = BlockHelpers.GetHeaderSize();
                mapAccessor.Write<BlockHeader>(0, ref block.Header);
                mapAccessor.WriteArray(headerSize, block.BlockBytes, 0, this.BlockSize - headerSize);
                mapAccessor.Flush();
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
                    this.mappedFile = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    this.mappedFile = MemoryMappedFile.CreateFromFile(this.FilePath, FileMode.OpenOrCreate, this.typeNameNormilized, this.Capacity, MemoryMappedFileAccess.ReadWrite);
                    this.freeBlocks.AddFreeBlockRange(firstAddressInNewArea, this.NumberOfBufferBlocks, this.BlockSize);

                    this.perfCounters.ResizeCounter.Increment();
                }
            }
        }

        private void UpdateIndeciesForInstance(YawnSchema oldRecord, YawnSchema newRecord, IBonded<StorageLocation> newLocation)
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

        public T ReadRecord(StorageLocation fromLocation)
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

            // ReadRecord:
            long location = blockStorageLocation.Address;
            long firstLocation = location;
            var cacheInstance = this.cache.Get(blockStorageLocation.Id.ToString());
            if (cacheInstance != null)
            {
                var lck = this.GetRecordLockName(this.SchemaType) + "_" + (cacheInstance as T)?.Id;
                if (this.yawnSite.RecordLocker.WaitForRecordLock(lck, Locking.RecordLockType.Read))
                {
                    cacheInstance = this.cache.Get(blockStorageLocation.Id.ToString());
                }

                StorageEventSource.Log.RecordReadFromCahe(this.FullStorageName, (cacheInstance as T).Id);
                this.perfCounters.RecordReadFromCacheCounter.Increment();
                return this.cloner.Clone<T>(cacheInstance as T);
            }

            StorageEventSource.Log.RecordReadStart(this.FullStorageName, blockStorageLocation.Address);
            this.perfCounters.RecordReadStartCounter.Increment();

            bool lastBlock = false;
            byte[] buffer = null;
            int bytesReadSofar = 0;
            using (var unlocker = new StorageUnlocker(this.recordLocker.LockRecord(blockStorageLocation.Id, this.resizeLock), this.recordLocker))
            {
                for (int i = 0; !lastBlock; i++)
                {
                    using (var mapAccessor = this.mappedFile.CreateViewAccessor(location, this.BlockSize))
                    {
                        BlockHeader header;
                        mapAccessor.Read<BlockHeader>(0, out header);
                        lastBlock = (header.BlockProperties & BlockProperties.IsLastBlockInRecord) != 0;
                        location = header.NextBlockLocation;
                        if (i == 0)
                        {
                            buffer = new byte[header.RecordSize];
                        }

                        var bytesInBlock = this.BytesOnBlock(header.RecordSize, lastBlock);
                        mapAccessor.ReadArray(BlockHelpers.GetHeaderSize(), buffer, bytesReadSofar, bytesInBlock);
                        bytesReadSofar += bytesInBlock;
                    }
                }
            }

            var input = new InputBuffer(buffer.ToArray());
            var reader = new CompactBinaryReader<InputBuffer>(input);
            T instance = this.schemaDeserializer.Deserialize<T>(reader);

            var lockName = this.GetRecordLockName(this.SchemaType) + "_" + instance.Id;
            if (this.yawnSite.RecordLocker.WaitForRecordLock(lockName, Locking.RecordLockType.Read))
            {
                instance = this.ReadRecord(fromLocation);
            }

            this.cache.Set(instance.Id.ToString(), instance, new CacheItemPolicy());
            StorageEventSource.Log.RecordSerializeFinish(this.FullStorageName, blockStorageLocation.Address);
            this.perfCounters.RecordReadFinishedCounter.Increment();
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

        public IEnumerable<TE> GetRecords<TE>(IEnumerable<IBonded<StorageLocation>> recordsToPull) where TE : YawnSchema
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

                yield return this.ReadRecord(location.Deserialize<BlockStorageLocation>()) as TE;
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
                this.nextIndex++;
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
            if (instance != null)
            {
                transactionItem.NewInstance = new Bonded<T>(instance as T);
            }

            transactionItem.OldInstance = new Bonded<YawnSchema>(new YawnSchema() { Id = -1 });
            transactionItem.ItemAction = Transactions.TransactionAction.Delete;
            transactionItem.Storage = this;
            transaction.AddTransactionItem(transactionItem);

            return this.yawnSite.SaveRecord(transaction as YawnSchema) == null ? false : true;
        }

        public bool DeleteRecord(YawnSchema instance)
        {
            var lck = this.GetRecordLockName(this.SchemaType) + "_" + instance.Id;
            this.yawnSite.RecordLocker.WaitForRecordLock(lck, Locking.RecordLockType.Write);

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
            StorageSyncLockCounter recordLock;
            lock (recordLock = this.recordLocker.LockRecord(instance.Id, this.resizeLock))
            {
                using (var unlocker = new StorageUnlocker(recordLock, this.recordLocker))
                {
                    while (!lastBlock)
                    {
                        using (var mapAccessor = this.mappedFile.CreateViewAccessor(location, this.BlockSize))
                        {
                            BlockHeader header;
                            mapAccessor.Read<BlockHeader>(0, out header);
                            if (header.RecordId != instance.Id)
                            {
                                return false;
                            }

                            lastBlock = (header.BlockProperties & BlockProperties.IsLastBlockInRecord) != 0;
                            mapAccessor.Write<BlockHeader>(0, ref blankHeader);

                            this.freeBlocks.AddFreeBlock(location);
                            location = header.NextBlockLocation;
                        }
                    }
                }
            }

            this.DeleteIndeciesForInstance(instance);
            this.cache.Remove(instance.Id.ToString());
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
                                index.SetIndex(record, new Bonded<BlockStorageLocation>(location));
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
                    index.Value.Close(true);
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

        public IEnumerable<IBonded<StorageLocation>> GetStorageLocations(IIdexArguments queryParams)
        {
            List<IBonded<StorageLocation>> locations = new List<IBonded<StorageLocation>>();
            foreach (var index in this.Indicies)
            {
                locations.AddRange(index.Value.GetStorageLocations(queryParams));
            }

            return locations;
        }

        public bool CommitTransactionItem(ITransactionItem transactionItem, IBonded bondedTransactionItem)
        {
            var item = bondedTransactionItem.Deserialize<BlockTransactionItem>();
            item.Storage = transactionItem.Storage;
            switch (item.ItemAction)
            {
                case Transactions.TransactionAction.Delete:
                    return this.DeleteRecord(item.NewInstance.Deserialize<T>());

                case Transactions.TransactionAction.Update:
                case Transactions.TransactionAction.Insert:
                    return this.CommitSave(item);

                default:
                    return false;
            }
        }

        public bool RollbackTransactionItem(ITransactionItem transactionItem, IBonded bondedTransactionItem)
        {
            var item = bondedTransactionItem.Deserialize<BlockTransactionItem>();

            item.Storage = transactionItem.Storage;
            switch (item.ItemAction)
            {
                case Transactions.TransactionAction.Update:
                case Transactions.TransactionAction.Insert:
                    return this.RollbackSave(item);

                // since delete did nothing on disk simply ignore
                case Transactions.TransactionAction.Delete:
                default:
                    return false;
            }
        }

        private long GetExistingAddress(YawnSchema instance)
        {
            var keyIndex = this.Indicies["YawnKeyIndex"];
            var existingLocation = keyIndex.GetLocationForInstance(instance).Deserialize<BlockStorageLocation>();
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

                // Thread.Yield();
                Thread.Sleep(0);
            }

            return someoneWasAlreadyResizing;
        }

        private string GetRecordLockName(Type type)
        {
            string name = type.FullName;
            if (type.IsGenericType)
            {
                name += "[";
                foreach (var arg in type.GetGenericArguments())
                {
                    name += this.GetRecordLockName(arg);
                }

                name += "]";
            }

            return name;
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
