namespace YawnDB.Storage.BlockStorage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;

    public static class BlockProperties
    {
        public const byte InUse = 0x1;                  // Bit 1
        public const byte IsFirstBlockInRecord = 0x2;   // Bit 2
        public const byte IsLastBlockInRecord = 0x4;    // Bit 3
        public const byte IsCommited = 0x8;             // Bit 4
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct BlockHeader
    {
        [FieldOffset(0)]
        public byte BlockProperties;

        [FieldOffset(1)]
        public long NextBlockLocation;

        [FieldOffset(9)]
        public long RecordSize;
    }

    public static class BlockHelpers
    {
        public static int GetHeaderSize()
        {
            return 17;
        }
    }

    public class Block
    {
        public BlockHeader Header;
        public long Address;
        public byte[] BlockBytes;
    }
}
