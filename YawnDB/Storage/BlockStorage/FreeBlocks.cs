namespace YawnDB.Storage.BlockStorage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using YawnDB.Storage;
    using Bond;
    using Bond.Protocols;
    using Bond.IO.Unsafe;

    partial class FreeBlocks 
    {

        private string FilePath;

        private object accessLock = new object();

        public FreeBlocks(string filePath)
        {
            this.FilePath = filePath;
            this.Blocks = new SortedSet<long>();

            if (File.Exists(filePath))
            {
                ReadFromFile();
            }
        }

        public void ScanFreeBlocksFromMap(MemoryMappedViewAccessor mapAccessor, long capacity, int blockSize)
        {
            lock (accessLock)
            {
                this.Blocks.Clear();
                int blocksInMap = (int)(capacity / blockSize);
                for (int i = 0; i < blocksInMap; i++)
                {
                    BlockHeader header;
                    long blockAddress = i * blockSize;
                    mapAccessor.Read<BlockHeader>(blockAddress, out header);
                    if((header.BlockProperties & BlockProperties.InUse) == 0)
                    {
                        this.Blocks.Add(blockAddress);
                    }
                }
            }
        }

        public void AddFreeBlockRange(long firstAddress, int nuberOfBlocks, int blockSize)
        {
            lock (accessLock)
            {
                var end = firstAddress + nuberOfBlocks * blockSize;
                for (long i = firstAddress; i < end; i += blockSize)
                {
                    if (!this.Blocks.Contains(i))
                    {
                        this.Blocks.Add(i);
                    }
                }
            }
        }

        public void AddFreeBlock(long blockAddress)
        {
            lock(accessLock)
            {
                if (!this.Blocks.Contains(blockAddress))
                {
                    this.Blocks.Add(blockAddress);
                }
            }
        }

        public bool PopFreeBlock(out long blockAddress)
        {
            lock (accessLock)
            {
                if(this.Blocks.Any())
                {
                    blockAddress = this.Blocks.First();
                    this.Blocks.Remove(blockAddress);
                    return true;
                }
            }

            blockAddress = 0;
            return false;
        }

        public void SaveToFile()
        {
            var output = new OutputBuffer();
            var writer = new CompactBinaryWriter<OutputBuffer>(output);
            Serializer<CompactBinaryWriter<OutputBuffer>> SchemaSerializer = new Serializer<CompactBinaryWriter<OutputBuffer>>(typeof(FreeBlocks));
            SchemaSerializer.Serialize(this, writer);
            File.WriteAllBytes(FilePath, output.Data.Array);
        }

        public void ReadFromFile()
        {
            var input = new InputBuffer(File.ReadAllBytes(this.FilePath));
            var reader = new CompactBinaryReader<InputBuffer>(input);
            Deserializer<CompactBinaryReader<InputBuffer>> SchemaDeserializer = new Deserializer<CompactBinaryReader<InputBuffer>>(typeof(FreeBlocks));
            this.Blocks = SchemaDeserializer.Deserialize<FreeBlocks>(reader).Blocks;
        }

    }
}
