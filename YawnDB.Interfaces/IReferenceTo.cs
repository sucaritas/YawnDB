namespace YawnDB.Interfaces
{
    using System.Collections.Generic;
    using System.Linq;

    public interface IReferenceTo<T> : IReference, IOrderedQueryable<T>, IQueryProvider  where T : YawnSchema
    {
    }
}
