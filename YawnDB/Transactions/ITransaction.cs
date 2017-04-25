// <copyright file="ITransaction.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Transactions
{
    using System;
    using System.Collections.Generic;
    using YawnDB.Storage;

    public interface ITransaction : IDisposable
    {
        IYawn YawnSite { get; set; }

        bool Commit();

        bool Rollback();

        StorageLocation SaveRecord(YawnSchema instanceToSave);

        bool DeleteRecord(YawnSchema instance);

        void AddTransactionItem<T>(T transactionItem) where T : TransactionItem;
    }
}
