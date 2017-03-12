// <copyright file="ReferenceTo.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using YawnDB.Exceptions;
    using YawnDB.Interfaces;

    public class ReferenceTo<T> : IReferenceTo<T> where T : YawnSchema
    {
        public IYawn YawnSite { get; set; }

        #region IQueryable
        public Type ElementType { get; }

        public Expression Expression { get; }

        public IQueryProvider Provider { get; }
        #endregion

        public ReferenceTo()
        {
            this.Provider = this;
            this.Expression = Expression.Constant(this);
        }

        #region IEnumerable interface
        public IEnumerator<T> GetEnumerator()
        {
            var exp = this.Expression as ConstantExpression;
            if (exp != null && exp.Value == this)
            {
                return this.YawnSite.RegisteredStorageTypes[typeof(T)].GetAllRecords<T>().GetEnumerator();
            }

            return Enumerable.Empty<T>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            IEnumerator<T> enumerator = this.GetEnumerator();
            return enumerator;
        }
        #endregion

        #region IQueryProvider
        public IQueryable CreateQuery(Expression expression)
        {
            return this.CreateQuery<T>(expression);
        }

        public IQueryable<TE> CreateQuery<TE>(Expression expression)
        {
            var tp = typeof(TE);
            if (tp == typeof(T))
            {
                return (IOrderedQueryable<TE>)this;
            }

            return this.Execute<IEnumerable<TE>>(expression).AsQueryable();
        }

        public object Execute(Expression expression)
        {
            return this.Execute<T>(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            LambdaExpression lambda = Expression.Lambda(new QueryProcessor<T>().ParseQuery(expression, this));
            var generatedDelegate = lambda.Compile();

            return (TResult)generatedDelegate.DynamicInvoke(null);
        }
        #endregion
    }
}
