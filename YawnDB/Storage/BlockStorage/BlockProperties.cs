// <copyright file="BlockProperties.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

#pragma warning disable SA1402 // File may only contain a single class
namespace YawnDB.Storage.BlockStorage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    public static class BlockProperties
    {
        public const byte InUse = 0x1;                  // Bit 1
        public const byte IsFirstBlockInRecord = 0x2;   // Bit 2
        public const byte IsLastBlockInRecord = 0x4;    // Bit 3
        public const byte IsCommited = 0x8;             // Bit 4
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1900:ValueTypeFieldsShouldBePortable", MessageId = "NextBlockLocation", Justification = "The whole project is non portable")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1900:ValueTypeFieldsShouldBePortable", MessageId = "RecordSize", Justification = "The whole project is non portable")]
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
#pragma warning disable SA1401 // Fields must be private
        public BlockHeader Header;

        public long Address;

        public byte[] BlockBytes;
#pragma warning restore SA1401 // Fields must be private
    }
}
#pragma warning restore SA1402 // File may only contain a single class