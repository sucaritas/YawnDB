// <copyright file="TransactionItem.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Transactions
{
    using System;
    using System.Collections.Generic;
    using YawnDB.Interfaces;

    public partial class TransactionItem : ITransactionItem
    {
        public IStorage Storage { get; set; }

        [global::Bond.Id(23)]
        public YawnSchema OldInstance { get; set; } = new YawnSchema();

        [global::Bond.Id(24)]
        public YawnSchema NewInstance { get; set; } = new YawnSchema();

        public bool Commit()
        {
            return this.Storage.CommitTransactionItem(this);
        }

        public bool Rollback()
        {
            return this.Storage.RollbackTransactionItem(this);
        }
    }
}
