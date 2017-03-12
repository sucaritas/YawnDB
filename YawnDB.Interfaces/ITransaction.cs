// <copyright file="ITransaction.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Interfaces
{
    using System;
    using System.Collections.Generic;

    public interface ITransaction : IDisposable
    {
        IYawn YawnSite { get; set; }

        bool Commit();

        bool Rollback();

        IStorageLocation SaveRecord(YawnSchema instanceToSave);

        bool DeleteRecord(YawnSchema instance);

        void AddTransactionItem(ITransactionItem transactionItem);
    }
}
