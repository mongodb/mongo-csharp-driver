/* Copyright 2010-2015 MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Static class that contains the Mongo Linq extension methods.
    /// </summary>
    public static class LinqExtensionMethods
    {
        /// <summary>
        /// Returns an instance of IQueryable{{T}} for a MongoCollection.
        /// </summary>
        /// <typeparam name="T">The type of the returned documents.</typeparam>
        /// <param name="collection">The name of the collection.</param>
        /// <returns>An instance of IQueryable{{T}} for a MongoCollection.</returns>
        public static IQueryable<T> AsQueryable<T>(this MongoCollection collection)
        {
            var provider = new MongoQueryProvider(collection);
            return new MongoQueryable<T>(provider);
        }

        /// <summary>
        /// Returns an instance of IQueryable{{T}} for a MongoCollection.
        /// </summary>
        /// <typeparam name="T">The type of the returned documents.</typeparam>
        /// <param name="collection">The name of the collection.</param>
        /// <returns>An instance of IQueryable{{T}} for a MongoCollection.</returns>
        public static IQueryable<T> AsQueryable<T>(this MongoCollection<T> collection)
        {
            var provider = new MongoQueryProvider(collection);
            return new MongoQueryable<T>(provider);
        }

        /// <summary>
        /// Returns an explanation of how the query was executed (instead of the results).
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The LINQ query to explain.</param>
        /// <returns>An explanation of thow the query was executed.</returns>
        public static BsonDocument Explain<TSource>(this IQueryable<TSource> source)
        {
            return Explain(source, false);
        }

        /// <summary>
        /// Returns an explanation of how the query was executed (instead of the results).
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The LINQ query to explain</param>
        /// <param name="verbose">Whether the explanation should contain more details.</param>
        /// <returns>An explanation of thow the query was executed.</returns>
        public static BsonDocument Explain<TSource>(this IQueryable<TSource> source, bool verbose)
        {
            var queryProvider = source.Provider as MongoQueryProvider;
            if (queryProvider == null)
            {
                throw new NotSupportedException("Explain can only be called on a Linq to Mongo queryable.");
            }

            var selectQuery = (SelectQuery)MongoQueryTranslator.Translate(queryProvider, source.Expression);
            if (selectQuery.Take.HasValue && selectQuery.Take.Value == 0)
            {
                throw new NotSupportedException("A query that has a .Take(0) expression will not be sent to the server and can't be explained");
            }
            var projector = selectQuery.Execute() as IProjector;
            if (projector == null)
            {
                // this is mainly for .Distinct() queries. First, Last, FirstOrDefault, LastOrDefault don't return
                // IQueryable<T>, so .Explain() can't be called on them anyway.
                throw new NotSupportedException("Explain can only be called on Linq queries that return an IProjector");
            }
            return projector.Cursor.Explain(verbose);
        }
    }
}
