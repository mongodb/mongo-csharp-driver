/* Copyright 2010-2016 MongoDB Inc.
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
using System.Reflection;
using MongoDB.Bson;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// This static class holds methods that can be used to express MongoDB specific query operations in LINQ queries.
    /// </summary>
    public static class LinqToMongo
    {
        // public static methods
        /// <summary>
        /// Determines whether a sequence contains all of the specified values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence in which to locate the values.</param>
        /// <param name="values">The values to locate in the sequence.</param>
        /// <returns>True if the sequence contains all of the specified values.</returns>
        public static bool ContainsAll<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> values)
        {
            return values.All(v => source.Contains(v));
        }

        /// <summary>
        /// Determines whether a sequence contains any of the specified values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence in which to locate the values.</param>
        /// <param name="values">The values to locate in the sequence.</param>
        /// <returns>True if the sequence contains any of the specified values.</returns>
        public static bool ContainsAny<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> values)
        {
            return source.Any(s => values.Contains(s));
        }

        /// <summary>
        /// Determines whether a specified value is contained in a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <param name="source">A sequence in which to locate the values.</param>
        /// <returns>True if the value is contained in the sequence.</returns>
        public static bool In<TSource>(this TSource value, IEnumerable<TSource> source)
        {
            return source.Contains(value);
        }

        /// <summary>
        /// Injects a low level IMongoQuery into a LINQ where clause. Can only be used in LINQ queries.
        /// </summary>
        /// <param name="query">The low level query.</param>
        /// <returns>Throws an InvalidOperationException if called.</returns>
        public static bool Inject(this IMongoQuery query)
        {
            throw new InvalidOperationException("The LinqToMongo.Inject method is only intended to be used in LINQ Where clauses.");
        }

        /// <summary>
        /// Sets an index hint on the query that's being built.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The query being built.</param>
        /// <param name="indexName">The name of the index to use.</param>
        /// <returns>New query where the expression includes a WithIndex method call.</returns>
        public static IQueryable<TSource> WithIndex<TSource>(this IQueryable<TSource> source, string indexName)
        {
            return WithIndex(source, (BsonValue)indexName);
        }

        /// <summary>
        /// Sets an index hint on the query that's being built.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The query being built.</param>
        /// <param name="indexHint">Hint for what index to use.</param>
        /// <returns>New query where the expression includes a WithIndex method call.</returns>
        public static IQueryable<TSource> WithIndex<TSource>(this IQueryable<TSource> source, BsonDocument indexHint)
        {
            return WithIndex(source, (BsonValue)indexHint);
        }

        // private static methods
        private static IQueryable<TSource> WithIndex<TSource>(IQueryable<TSource> query, BsonValue indexHint)
        {
            var methodDefinition = typeof(LinqToMongo).GetTypeInfo().GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == "WithIndex")
                .Where(m => { var ga = m.GetGenericArguments(); return ga.Length == 1 && ga[0].Name == "TSource"; })
                .Where(m => { var p = m.GetParameters(); return p.Length == 2 && p[1].ParameterType == typeof(BsonValue); })
                .Single();
            var method = methodDefinition.MakeGenericMethod(typeof(TSource));
            var args = new[] { query.Expression, Expression.Constant(indexHint) };
            var expression = Expression.Call(null, method, args);
            return query.Provider.CreateQuery<TSource>(expression);
        }
    }
}
