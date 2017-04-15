// <copyright file="IReferenceTo.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB
{
    using System.Collections.Generic;
    using System.Linq;

    public interface IReferenceTo<T> : IReference, IOrderedQueryable<T>, IQueryProvider
        where T : YawnSchema
    {
    }
}
