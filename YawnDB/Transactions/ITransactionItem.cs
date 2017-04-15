// <copyright file="ITransactionItem.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Transactions
{
    using Bond;
    using YawnDB.Storage;

    public interface ITransactionItem
    {
        IStorage Storage { get; set; }

        bool Commit(IBonded bondedTransactionItem);

        bool Rollback(IBonded bondedTransactionItem);
    }
}
