namespace YawnDB
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;
    using System.Reflection;
    using YawnDB.Interfaces;

    class QueryProcessor<T> : ExpressionVisitor where T : YawnSchema
    {
        private ReferenceTo<T> SchemaReference;
        private Type SchemaType = typeof(T);
        private Type ReferenceType = typeof(T);

        public Expression ParseQuery(Expression expression, ReferenceTo<T> schemaReference)
        {
            this.SchemaReference = schemaReference;
            return Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var backingStorage = SchemaReference.YawnSite.RegisteredStorageTypes[SchemaType];
            var getAllrecordsMethod = backingStorage.GetType().GetMethod("GetAllRecords").MakeGenericMethod(SchemaType);
            var storageEnumerable = Expression.Call(Expression.Constant(backingStorage), getAllrecordsMethod);
            var queriableMethod = typeof(System.Linq.Queryable).GetMethods().First(x=>x.Name== "AsQueryable" && x.IsGenericMethod).MakeGenericMethod(SchemaType);
            var storageQueriable = Expression.Call(queriableMethod, storageEnumerable);

            // Replace instance if this is a method call
            // In theory this should not happen as this is a linq query and methods are static (extensions)
            if (node.Object != null && node.Object.GetType() == SchemaType)
            {
                node = Expression.Call(storageQueriable, node.Method, node.Arguments.ToArray());
            }

            // If the "node.Object" is null the this is a static method call
            // therefore the first argument must be the ReferenceTo<T> object which needs to be replaced
            if (node.Object == null && node.Arguments.First().Type == typeof(ReferenceTo<T>))
            {
                var newArgumentList = new List<Expression>()
                {
                    storageQueriable
                };

                newArgumentList.AddRange(node.Arguments.Skip(1));

                if (node.Object == null)
                {
                    node = Expression.Call(node.Method, newArgumentList.ToArray());
                }
                else
                {
                    node = Expression.Call(node.Object, node.Method, newArgumentList.ToArray());
                }
            }


            return node;
        }
    }
}
