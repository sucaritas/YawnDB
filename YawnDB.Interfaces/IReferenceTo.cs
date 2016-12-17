namespace YawnDB.Interfaces
{
    using System.Collections.Generic;

    public interface IReferenceTo<T> : IReference, ICollection<T> where T : YawnSchema
    {
    }
}
