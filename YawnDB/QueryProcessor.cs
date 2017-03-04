﻿namespace YawnDB
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
        private static Type SchemaType = typeof(T);
        private static Type ReferencingType = typeof(YawnSchema).Assembly.GetType("YawnDB.Referencing`1["+ typeof(T).FullName + "]");
        private static PropertyInfo RefrencedIdsProperty = ReferencingType?.GetProperty("RefrencedIds");

        public Expression ParseQuery(Expression expression, ReferenceTo<T> schemaReference)
        {
            this.SchemaReference = schemaReference;
            return Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var backingStorage = SchemaReference.YawnSite.RegisteredStorageTypes[SchemaType];
            MethodInfo getRecordsMethod = null;
            MethodCallExpression storageEnumerable = null;

            // if this instance is a referencing instance than get all referenced id and add them to the expression
            if (ReferencingType.IsAssignableFrom(this.SchemaReference.GetType()))
            {
                var indexParametersType = typeof(IndexParameters);
                var argumentConstructor = Expression.New(indexParametersType);
                var valueMember = indexParametersType.GetMember("Value1").First();
                var valueMemberBind = Expression.Bind(valueMember, Expression.Constant(new Dictionary<string, object>() { { "Id", RefrencedIdsProperty.GetValue(this.SchemaReference) } }));
                var indexParamsMember = indexParametersType.GetMember("IndexParams").First();
                var indexParamsMemberBind = Expression.Bind(indexParamsMember, Expression.Constant(new List<string>() { "Id" }));

                var idsExpression = Expression.MemberInit(argumentConstructor, indexParamsMemberBind, valueMemberBind);
                getRecordsMethod = backingStorage.GetType().GetMethod("GetRecords").MakeGenericMethod(SchemaType);
                var getStorageLocationsMethod = backingStorage.GetType().GetMethod("GetStorageLocations");
                var callGetStorageLocations = Expression.Call(Expression.Constant(backingStorage), getStorageLocationsMethod, idsExpression);
                storageEnumerable = Expression.Call(Expression.Constant(backingStorage), getRecordsMethod, callGetStorageLocations);
            }
            // Else enumerate all records
            else
            {
                getRecordsMethod = backingStorage.GetType().GetMethod("GetAllRecords").MakeGenericMethod(SchemaType);
                storageEnumerable = Expression.Call(Expression.Constant(backingStorage), getRecordsMethod);
            }

            var queriableMethod = typeof(System.Linq.Queryable).GetMethods().First(x=>x.Name== "AsQueryable" && x.IsGenericMethod).MakeGenericMethod(SchemaType);
            var storageQueriable = Expression.Call(queriableMethod, storageEnumerable);

            // Replace the instance, if this is a method call
            // In theory this should not happen as this is a linq query and methods are static (extensions)
            if (node.Object != null && node.Object.GetType() == SchemaType)
            {
                node = Expression.Call(storageQueriable, node.Method, node.Arguments.ToArray());
            }

            // If the "node.Object" is null then this is a static method call
            // therefore the first argument must be the ReferenceTo<T> object which needs to be replaced
            if (node.Object == null && ReferencingType.IsAssignableFrom(node.Arguments.First().Type))
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
