namespace YawnDB.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Bond;

    public interface IStorageOf<T> : IStorage where T : YawnSchema
    {
    }
}
