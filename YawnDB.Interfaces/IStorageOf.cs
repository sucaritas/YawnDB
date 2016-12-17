namespace YawnDB.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Bond;

    public interface IStorageOf<T> : IStorage where T : YawnSchema
    {
        IDictionary<string, IIndex> Indicies { get; }

        Type SchemaType { get; }

        Task<IStorageLocation> SaveRecord(T instanceToSave);

        bool DeleteRecord(T instance);

        Task<IEnumerable<T>> GetRecords(IEnumerable<IStorageLocation> recordsToPull);

        Task<IEnumerable<T>> GetAllRecords();

        Task<T> CreateRecord();

        long GetNextID();

        void ReIndexStorage(IList<IIndex> needReindexing);

    }
}
