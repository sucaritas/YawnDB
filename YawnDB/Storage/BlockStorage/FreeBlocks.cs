// <copyright file="FreeBlocks.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Storage.BlockStorage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Bond;
    using Bond.IO.Unsafe;
    using Bond.Protocols;
    using YawnDB.Storage;

    public partial class FreeBlocks
    {
        private string filePath;

        private object accessLock = new object();

        public FreeBlocks(string filePath)
        {
            this.filePath = filePath;
            this.Blocks = new SortedSet<long>();

            if (File.Exists(filePath))
            {
                this.ReadFromFile();
            }
        }

        public void ScanFreeBlocksFromMap(MemoryMappedViewAccessor mapAccessor, long capacity, int blockSize)
        {
            lock (this.accessLock)
            {
                this.Blocks.Clear();
                int blocksInMap = (int)(capacity / blockSize);
                for (int i = 0; i < blocksInMap; i++)
                {
                    BlockHeader header;
                    long blockAddress = i * blockSize;
                    mapAccessor.Read<BlockHeader>(blockAddress, out header);
                    if ((header.BlockProperties & BlockProperties.InUse) == 0)
                    {
                        this.Blocks.Add(blockAddress);
                    }
                }
            }
        }

        public void AddFreeBlockRange(long firstAddress, int nuberOfBlocks, int blockSize)
        {
            lock (this.accessLock)
            {
                var end = firstAddress + (nuberOfBlocks * blockSize);
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
            lock (this.accessLock)
            {
                if (!this.Blocks.Contains(blockAddress))
                {
                    this.Blocks.Add(blockAddress);
                }
            }
        }

        public bool PopFreeBlock(out long blockAddress)
        {
            lock (this.accessLock)
            {
                if (this.Blocks.Any())
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
            Serializer<CompactBinaryWriter<OutputBuffer>> schemaSerializer = new Serializer<CompactBinaryWriter<OutputBuffer>>(typeof(FreeBlocks));
            schemaSerializer.Serialize(this, writer);
            File.WriteAllBytes(this.filePath, output.Data.Array);
        }

        public void ReadFromFile()
        {
            var input = new InputBuffer(File.ReadAllBytes(this.filePath));
            var reader = new CompactBinaryReader<InputBuffer>(input);
            Deserializer<CompactBinaryReader<InputBuffer>> schemaDeserializer = new Deserializer<CompactBinaryReader<InputBuffer>>(typeof(FreeBlocks));
            this.Blocks = schemaDeserializer.Deserialize<FreeBlocks>(reader).Blocks;
        }
    }
}
