// <copyright file="MemStorageLocation.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Storage.MemStorage
{
    using YawnDB.Interfaces;

    public class MemStorageLocation : IStorageLocation
    {
        public long Id { get; set; }
    }
}
