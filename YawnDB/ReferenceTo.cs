namespace YawnDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using YawnDB.Interfaces;
    using YawnDB.Exceptions;
    using System.Linq.Expressions;

    public class ReferenceTo<T> : IReferenceTo<T> where T : YawnSchema
    {
        private IYawn YawnSite;


        public void SetYawnSite(IYawn site)
        {
            this.YawnSite = site;
        }

        public void ResetYawnSite()
        {
            this.YawnSite = null;
        }

        #region ICollection Interface
        public int Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #region ICollection interface methods
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (this.YawnSite == null)
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }

            IStorage storage;
            this.YawnSite.TryGetStorage(typeof(T), out storage);
            IStorageOf<T> typedStorage = storage as IStorageOf<T>;

            if (typedStorage == null)
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }

            return typedStorage.GetAllRecords().Result.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (this.YawnSite == null)
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }

            IStorage storage;
            this.YawnSite.TryGetStorage(typeof(T), out storage);
            IStorageOf<T> typedStorage = storage as IStorageOf<T>;

            if (typedStorage == null)
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }

            return typedStorage.GetAllRecords().Result.GetEnumerator();
        }
        #endregion
        
        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
