// <copyright file="IStorage.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Bond;
    using YawnDB.Index;
    using YawnDB.Transactions;

    public interface IStorage
    {
        IDictionary<string, IIndex> Indicies { get; }

        Type SchemaType { get; }

        StorageState State { get; }

        IEnumerable<TE> GetRecords<TE>(IEnumerable<IBonded<StorageLocation>> recordsToPull) where TE : YawnSchema;

        IEnumerable<TE> GetAllRecords<TE>() where TE : YawnSchema;

        IEnumerable<IBonded<StorageLocation>> GetStorageLocations(IIdexArguments queryParams);

        void Open();

        void Close();

        StorageLocation InsertRecord(YawnSchema instanceToInsert);

        StorageLocation InsertRecord(YawnSchema instanceToInsert, ITransaction transaction);

        StorageLocation SaveRecord(YawnSchema instanceToSave);

        StorageLocation SaveRecord(YawnSchema instanceToSave, ITransaction transaction);

        bool DeleteRecord(YawnSchema instance);

        bool DeleteRecord(YawnSchema instance, ITransaction transaction);

        YawnSchema CreateRecord();

        long GetNextID();

        void ReIndexStorage(IList<IIndex> needReindexing);

        bool CommitTransactionItem(ITransactionItem transactionItem, IBonded bondedTransactionItem);

        bool RollbackTransactionItem(ITransactionItem transactionItem, IBonded bondedTransactionItem);
    }
}
