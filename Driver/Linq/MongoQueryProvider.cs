/* Copyright 2010-2012 10gen Inc.
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
using System.Reflection;

// for a good blog post on implementing LINQ query providers see Matt Warren's blog posts
// see: http://blogs.msdn.com/b/mattwar/archive/2008/11/18/linq-links.aspx

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// An implementation of IQueryProvider for querying a MongoDB collection.
    /// </summary>
    public class MongoQueryProvider : IQueryProvider
    {
        // private fields
        private MongoCollection _collection;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoQueryProvider class.
        /// </summary>
        /// <param name="collection">The collection being queried.</param>
        public MongoQueryProvider(MongoCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            _collection = collection;
        }

        // public properties
        /// <summary>
        /// Gets the Collection.
        /// </summary>
        public MongoCollection Collection
        {
            get { return _collection; }
        }

        // public methods
        /// <summary>
        /// Builds the MongoDB query that will be sent to the server when the LINQ query is executed.
        /// </summary>
        /// <typeparam name="T">The type of the documents being queried.</typeparam>
        /// <param name="query">The LINQ query.</param>
        /// <returns>The MongoDB query.</returns>
        public IMongoQuery BuildMongoQuery<T>(MongoQueryable<T> query)
        {
            var translatedQuery = MongoQueryTranslator.Translate(this, ((IQueryable)query).Expression);
            return ((SelectQuery)translatedQuery).BuildQuery();
        }

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
            var elementType = TypeHelper.GetElementType(expression.Type);
            try
            {
                var queryableType = typeof(MongoQueryable<>).MakeGenericType(elementType);
                return (IQueryable)Activator.CreateInstance(queryableType, new object[] { this, expression });
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
            return (TResult)Execute(expression);
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

            var translatedQuery = MongoQueryTranslator.Translate(this, expression);
            return translatedQuery.Execute();
        }

        // private methods
        internal bool CanBeEvaluatedLocally(Expression expression)
        {
            // any operation on a query can't be done locally
            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
            {
                var query = constantExpression.Value as IQueryable;
                if (query != null && query.Provider == this)
                {
                    return false;
                }
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null)
            {
                var declaringType = methodCallExpression.Method.DeclaringType;
                if (declaringType == typeof(Enumerable) || declaringType == typeof(Queryable))
                {
                    return false;
                }
                if (declaringType == typeof(LinqToMongo))
                {
                    return false;
                }
            }

            if (expression.NodeType == ExpressionType.Convert && expression.Type == typeof(object))
            {
                return true;
            }

            if (expression.NodeType == ExpressionType.Parameter || expression.NodeType == ExpressionType.Lambda)
            {
                return false;
            }

            return true;
        }
    }
}
