﻿// <copyright file="ITransactionItem.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Interfaces
{
    public interface ITransactionItem
    {
        IStorage Storage { get; set; }

        bool Commit();

        bool Rollback();
    }
}
