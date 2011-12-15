/* Copyright 2010-2011 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Reflection;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// An implementation of IQueryProvider for querying a MongoDB collection.
    /// </summary>
    public class MongoQueryProvider : IQueryProvider
    {
        // private static fields
        private static Dictionary<Type, Func<MongoQueryProvider, Expression, IQueryable>> createQueryDelegates = new Dictionary<Type, Func<MongoQueryProvider, Expression, IQueryable>>();
        private static MethodInfo createQueryGenericMethodDefinition;
        private static Dictionary<Type, Func<MongoQueryProvider, Expression, object>> executeDelegates = new Dictionary<Type, Func<MongoQueryProvider, Expression, object>>();
        private static MethodInfo executeGenericMethodDefinition;
        private static object staticLock = new object();

        // private fields
        private MongoCollection collection;

        // static constructor
        static MongoQueryProvider()
        {
            foreach (var methodInfo in typeof(MongoQueryProvider).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (methodInfo.Name == "CreateQuery" && methodInfo.IsGenericMethodDefinition)
                {
                    createQueryGenericMethodDefinition = methodInfo;
                }
                if (methodInfo.Name == "Execute" && methodInfo.IsGenericMethodDefinition)
                {
                    executeGenericMethodDefinition = methodInfo;
                }
            }
        }

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoQueryProvider class.
        /// </summary>
        public MongoQueryProvider(MongoCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            this.collection = collection;
        }

        // private static methods
        private static Func<MongoQueryProvider, Expression, IQueryable> GetCreateQueryDelegate(Type type)
        {
            lock (staticLock)
            {
                Func<MongoQueryProvider, Expression, IQueryable> createQueryDelegate;
                if (!createQueryDelegates.TryGetValue(type, out createQueryDelegate))
                {
                    var createQueryMethodInfo = createQueryGenericMethodDefinition.MakeGenericMethod(type);

                    // lambdaExpression = (provider, expression) => (IQueryable) provider.CreateQuery<T>(expression)
                    var providerParameter = Expression.Parameter(typeof(MongoQueryProvider), "provider");
                    var expressionParameter = Expression.Parameter(typeof(Expression), "expression");
                    var lambdaExpression = Expression.Lambda<Func<MongoQueryProvider, Expression, IQueryable>>(
                        Expression.Convert(
                            Expression.Call(providerParameter, createQueryMethodInfo, expressionParameter),
                            typeof(IQueryable)
                        ),
                        providerParameter,
                        expressionParameter
                    );
                    createQueryDelegate = lambdaExpression.Compile();
                    createQueryDelegates.Add(type, createQueryDelegate);
                }
                return createQueryDelegate;
            }
        }

        private static Func<MongoQueryProvider, Expression, object> GetExecuteDelegate(Type type)
        {
            lock (staticLock)
            {
                Func<MongoQueryProvider, Expression, object> executeDelegate;
                if (!executeDelegates.TryGetValue(type, out executeDelegate))
                {
                    var executeMethodInfo = executeGenericMethodDefinition.MakeGenericMethod(type);

                    // lambdaExpression = (provider, expression) => (object) provider.Execute<T>(expression)
                    var providerParameter = Expression.Parameter(typeof(MongoQueryProvider), "provider");
                    var expressionParameter = Expression.Parameter(typeof(Expression), "expression");
                    var lambdaExpression = Expression.Lambda<Func<MongoQueryProvider, Expression, object>>(
                        Expression.Convert(
                            Expression.Call(providerParameter, executeMethodInfo, expressionParameter),
                            typeof(object)
                        ),
                        providerParameter,
                        expressionParameter
                    );
                    executeDelegate = lambdaExpression.Compile();
                    executeDelegates.Add(type, executeDelegate);
                }
                return executeDelegate;
            }
        }

        // public methods
        /// <summary>
        /// Creates a new instance of MongoQueryable{{T}} for this provider.
        /// </summary>
        /// <typeparam name="T">The type of the returned elements.</typeparam>
        /// <param name="expression">The query expression.</param>
        /// <returns>A new instance of MongoQueryable{{T}}.</returns>
        public IQueryable<T> CreateQuery<T>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            return new MongoQueryable<T>(this, expression);
        }

        /// <summary>
        /// Creates a new instance MongoQueryable{{T}} for this provider. Calls the generic CreateQuery{{T}} 
        /// to actually create the new MongoQueryable{{T}} instance.
        /// </summary>
        /// <param name="expression">The query expression.</param>
        /// <returns>A new instance of MongoQueryable{{T}}.</returns>
        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            try
            {
                var elementType = TypeSystem.GetElementType(expression.Type);
                var createQueryDelegate = GetCreateQueryDelegate(elementType);
                return createQueryDelegate(this, expression);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="expression">The query expression.</param>
        /// <returns>The result of the query.</returns>
        public TResult Execute<TResult>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (!typeof(TResult).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException("Argument expression is not valid.");
            }
            var translatedQuery = MongoLinqTranslator.Translate(collection, expression);
            return (TResult)translatedQuery.Execute();
        }

        /// <summary>
        /// Executes a query. Calls the generic method Execute{{T}} to actually execute the query.
        /// </summary>
        /// <param name="expression">The query expression.</param>
        /// <returns>The result of the query.</returns>
        public object Execute(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            try
            {
                var resultType = expression.Type;
                var executeDelegate = GetExecuteDelegate(resultType);
                return executeDelegate(this, expression);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Gets an enumerator by executing the query.
        /// </summary>
        /// <typeparam name="T">Type element type.</typeparam>
        /// <param name="expression">The LINQ expression.</param>
        /// <returns>An enumerator for the results of the query.</returns>
        public IEnumerator<T> GetEnumerator<T>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (!typeof(IEnumerable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException("Argument expression is not valid.");
            }
            var translatedQuery = MongoLinqTranslator.Translate(collection, expression);
            return translatedQuery.GetEnumerator<T>();
        }

        /// <summary>
        /// Gets a string representation of the LINQ expression translated to a MongoDB query.
        /// </summary>
        /// <param name="expression">The LINQ expression.</param>
        /// <returns>A string.</returns>
        public string GetQueryText(Expression expression)
        {
            var translatedQuery = MongoLinqTranslator.Translate(collection, expression);
            return translatedQuery.ToString();
        }
    }
}
