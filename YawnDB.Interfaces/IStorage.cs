using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YawnDB.Interfaces
{
    public interface IStorage
    {
        IDictionary<string, IIndex> Indicies { get; }
        Type SchemaType { get; }
        StorageState State { get; }
        IEnumerable<TE> GetRecords<TE>(IEnumerable<IStorageLocation> recordsToPull) where TE : YawnSchema;
        IEnumerable<TE> GetAllRecords<TE>() where TE : YawnSchema;
        IEnumerable<IStorageLocation> GetStorageLocations(IIdexArguments queryParams);
        void Open();
        void Close();
        IStorageLocation InsertRecord(YawnSchema instanceToInsert);
        IStorageLocation InsertRecord(YawnSchema instanceToInsert, ITransaction transaction);
        IStorageLocation SaveRecord(YawnSchema instanceToSave);
        IStorageLocation SaveRecord(YawnSchema instanceToSave, ITransaction transaction);
        bool DeleteRecord(YawnSchema instance);
        bool DeleteRecord(YawnSchema instance, ITransaction transaction);
        YawnSchema CreateRecord();
        long GetNextID();
        void ReIndexStorage(IList<IIndex> needReindexing);
        bool CommitTransactionItem(ITransactionItem transactionItem);
        bool RollbackTransactionItem(ITransactionItem transactionItem);
    }
}
