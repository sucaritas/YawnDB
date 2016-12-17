namespace YawnDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;
    using YawnDB.Interfaces;


    public class QueryParser : IQueryProvider
    {
        private readonly Type internalType;
        private readonly IReference referenceToSite;
        private IYawn YawnSite;

        public QueryParser(IReference referenceToSite, Type internalType)
        {
            this.internalType = internalType;
            this.referenceToSite = referenceToSite;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(ReferenceTo<>).MakeGenericType(this.internalType), new object[] { this, expression });
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public void SetYawnSite(IYawn site)
        {
            this.YawnSite = site;
        }

        public void ResetYawnSite()
        {
            this.YawnSite = null;
        }

        // Queryable's collection-returning standard query operators call this method. 
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            Console.WriteLine(expression.ToString());
            IStorage storage;
            this.YawnSite.TryGetStorage(typeof(TElement), out storage);
            return (storage as IStorageOf<TElement>).ExecuteQuery().AsQueryable<TElement>();
        }

        // Queryable's "single value" standard query operators call this method.
        public TResult Execute<TResult>(Expression expression)
        {
            Console.WriteLine(expression.ToString());
            IStorage storage;
            this.YawnSite.TryGetStorage(typeof(T), out storage);
            var items = (storage as IStorageOf<T>).ExecuteQuery();
            return (TResult)(object)items;
        }
    }
}
