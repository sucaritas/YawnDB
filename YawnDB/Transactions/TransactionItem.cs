// <copyright file="TransactionItem.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Transactions
{
    using System;
    using System.Collections.Generic;
    using Bond;
    using YawnDB.Storage;

    public partial class TransactionItem : ITransactionItem
    {
        public IStorage Storage { get; set; }

        [global::Bond.Id(23)]
        public IBonded<YawnSchema> OldInstance { get; set; } = new Bonded<YawnSchema>(new YawnSchema() { Id = -1 });

        [global::Bond.Id(24)]
        public IBonded<YawnSchema> NewInstance { get; set; } = new Bonded<YawnSchema>(new YawnSchema() { Id = -1 });

        public bool Commit(IBonded bondedTransactionItem)
        {
            return this.Storage.CommitTransactionItem(this, bondedTransactionItem);
        }

        public bool Rollback(IBonded bondedTransactionItem)
        {
            return this.Storage.RollbackTransactionItem(this, bondedTransactionItem);
        }
    }
}
