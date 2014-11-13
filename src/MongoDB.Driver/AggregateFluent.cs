/* Copyright 2010-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Fluent interface for aggregate.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class AggregateFluent<TDocument, TResult> : IAsyncCursorSource<TResult>
    {
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly AggregateOptions _options;
        private readonly List<object> _pipeline;
        private readonly IBsonSerializer<TResult> _resultSerializer;
        private readonly Func<object, BsonDocument> _toBsonDocument;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateFluent{TDocument, TResult}" /> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="toBsonDocument">To bson document.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="options">The options.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public AggregateFluent(IMongoCollection<TDocument> collection, Func<object, BsonDocument> toBsonDocument, List<object> pipeline, AggregateOptions options, IBsonSerializer<TResult> resultSerializer)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _toBsonDocument = Ensure.IsNotNull(toBsonDocument, "toBsonDocument");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline");
            _options = Ensure.IsNotNull(options, "options");
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
        }

        // properties
        /// <summary>
        /// Gets the collection.
        /// </summary>
        public IMongoCollection<TDocument> Collection
        {
            get { return _collection; }
        }

        /// <summary>
        /// Gets the options.
        /// </summary>
        public AggregateOptions Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        public IList<object> Pipeline
        {
            get { return _pipeline; }
        }

        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        // methods
        /// <summary>
        /// Appends the specified stage.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> AppendStage(object stage)
        {
            _pipeline.Add(stage);
            return this;
        }

        /// <summary>
        /// Geoes the near.
        /// </summary>
        /// <param name="geoNear">The geo near.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> GeoNear(object geoNear)
        {
            return AppendStage(new BsonDocument("$geoNear", _toBsonDocument(geoNear)));
        }

        /// <summary>
        /// Groups the specified group.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Group(object group)
        {
            return AppendStage(new BsonDocument("$group", _toBsonDocument(group)));
        }

        /// <summary>
        /// Limits the specified limit.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Limit(int limit)
        {
            return AppendStage(new BsonDocument("$limit", limit));
        }

        /// <summary>
        /// Matches the specified match.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Match(object match)
        {
            return AppendStage(new BsonDocument("$match", _toBsonDocument(match)));
        }

        /// <summary>
        /// Outs the specified collection name.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Out(string collectionName)
        {
            return AppendStage(new BsonDocument("$out", collectionName));
        }

        /// <summary>
        /// Projects the specified project.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="project">The project.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project)
        {
            return Project<TNewResult>(project, null);
        }

        /// <summary>
        /// Projects the specified project.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="project">The project.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project, IBsonSerializer<TNewResult> resultSerializer)
        {
            AppendStage(new BsonDocument("$project", _toBsonDocument(project)));

            return CloneWithNewResultType<TNewResult>(resultSerializer);
        }

        /// <summary>
        /// Redacts the specified redact.
        /// </summary>
        /// <param name="redact">The redact.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Redact(object redact)
        {
            return AppendStage(new BsonDocument("$redact", _toBsonDocument(redact)));
        }

        /// <summary>
        /// Skips the specified skip.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Skip(int skip)
        {
            return AppendStage(new BsonDocument("$skip", skip));
        }

        /// <summary>
        /// Sorts the specified sort.
        /// </summary>
        /// <param name="sort">The sort.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Sort(object sort)
        {
            return AppendStage(new BsonDocument("$sort", _toBsonDocument(sort)));
        }

        /// <summary>
        /// Unwinds the specified field name.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TNewResult> Unwind<TNewResult>(string fieldName)
        {
            return Unwind<TNewResult>(fieldName, null);
        }

        /// <summary>
        /// Unwinds the specified field name.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TNewResult> Unwind<TNewResult>(string fieldName, IBsonSerializer<TNewResult> resultSerializer)
        {
            AppendStage(new BsonDocument("$unwind", fieldName));
            return CloneWithNewResultType<TNewResult>(resultSerializer);
        }

        /// <summary>
        /// Executes the aggregate operation and returns a cursor.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An asynchronous enumerable.</returns>
        public Task<IAsyncCursor<TResult>> ToCursorAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var options = new AggregateOptions<TResult>
            {
                AllowDiskUse = _options.AllowDiskUse,
                BatchSize = _options.BatchSize,
                MaxTime = _options.MaxTime,
                ResultSerializer = _resultSerializer,
                UseCursor = _options.UseCursor
            };
            return _collection.AggregateAsync(_pipeline, options, cancellationToken);
        }

        private AggregateFluent<TDocument, TNewResult> CloneWithNewResultType<TNewResult>(IBsonSerializer<TNewResult> resultSerializer)
        {
            return new AggregateFluent<TDocument, TNewResult>(_collection, _toBsonDocument, _pipeline, _options, resultSerializer);
        }
    }

    /// <summary>
    /// Extension methods for <see cref="AggregateFluent{TDocument, TResult}"/>
    /// </summary>
    public static class AggregateFluentExtensionMethods
    {
        /// <summary>
        /// Firsts the asynchronous.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        public async static Task<TResult> FirstAsync<TDocument, TResult>(this AggregateFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(1).ToCursorAsync(cancellationToken))
            {
                if (await cursor.MoveNextAsync(cancellationToken))
                {
                    return cursor.Current.First();
                }
                else
                {
                    throw new InvalidOperationException("The source sequence is empty.");
                }
            }
        }

        /// <summary>
        /// Firsts the or default asynchronous.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async static Task<TResult> FirstOrDefaultAsync<TDocument, TResult>(this AggregateFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(1).ToCursorAsync(cancellationToken))
            {
                if (await cursor.MoveNextAsync(cancellationToken))
                {
                    return cursor.Current.FirstOrDefault();
                }
                else
                {
                    return default(TResult);
                }
            }
        }

        /// <summary>
        /// Singles the asynchronous.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The source sequence is empty.</exception>
        public async static Task<TResult> SingleAsync<TDocument, TResult>(this AggregateFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(2).ToCursorAsync(cancellationToken))
            {
                if (await cursor.MoveNextAsync(cancellationToken))
                {
                    return cursor.Current.Single();
                }
                else
                {
                    throw new InvalidOperationException("The source sequence is empty.");
                }
            }
        }

        /// <summary>
        /// Singles the or default asynchronous.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async static Task<TResult> SingleOrDefaultAsync<TDocument, TResult>(this AggregateFluent<TDocument, TResult> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            using (var cursor = await source.Limit(2).ToCursorAsync(cancellationToken))
            {
                if (await cursor.MoveNextAsync(cancellationToken))
                {
                    return cursor.Current.SingleOrDefault();
                }
                else
                {
                    return default(TResult);
                }
            }
        }
    }
}
