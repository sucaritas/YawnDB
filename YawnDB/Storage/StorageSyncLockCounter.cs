// <copyright file="StorageSyncLockCounter.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Storage
{
    public class StorageSyncLockCounter
    {
#pragma warning disable SA1401 // Fields must be private
        public long Id;
        public int Readers = 0;
#pragma warning restore SA1401 // Fields must be private
    }
}
