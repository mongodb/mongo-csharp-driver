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
using System.Text;
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
    public class AggregateFluent<TDocument, TResult>
    {
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly AggregateOptions<TResult> _options;
        private readonly Func<object, BsonDocument> _toBsonDocument;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateFluent{TDocument, TResult}" /> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="toBsonDocument">To bson document.</param>
        public AggregateFluent(IMongoCollection<TDocument> collection, Func<object, BsonDocument> toBsonDocument)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _toBsonDocument = Ensure.IsNotNull(toBsonDocument, "toBsonDocument");
            _options = new AggregateOptions<TResult>();
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
        public AggregateOptions<TResult> Options
        {
            get { return _options; }
        }

        // methods
        /// <summary>
        /// Allows the disk use.
        /// </summary>
        /// <param name="allowDiskUse">The allow disk use.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> AllowDiskUse(bool? allowDiskUse)
        {
            _options.AllowDiskUse = allowDiskUse;
            return this;
        }

        /// <summary>
        /// Appends the specified stage.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Append(object stage)
        {
            _options.Pipeline.Add(stage);
            return this;
        }

        /// <summary>
        /// Batches the size.
        /// </summary>
        /// <param name="batchSize">Size of the batch.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> BatchSize(int? batchSize)
        {
            _options.BatchSize = batchSize;
            return this;
        }

        /// <summary>
        /// Geoes the near.
        /// </summary>
        /// <param name="geoNear">The geo near.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> GeoNear(object geoNear)
        {
            return Append(new BsonDocument("$geoNear", _toBsonDocument(geoNear)));
        }

        /// <summary>
        /// Groups the specified group.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Group(object group)
        {
            return Append(new BsonDocument("$group", _toBsonDocument(group)));
        }

        /// <summary>
        /// Limits the specified limit.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Limit(int limit)
        {
            return Append(new BsonDocument("$limit", limit));
        }

        /// <summary>
        /// Matches the specified match.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Match(object match)
        {
            return Append(new BsonDocument("$match", _toBsonDocument(match)));
        }

        /// <summary>
        /// Maximums the time.
        /// </summary>
        /// <param name="maxTime">The maximum time.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> MaxTime(TimeSpan? maxTime)
        {
            _options.MaxTime = maxTime;
            return this;
        }

        /// <summary>
        /// Outs the specified collection name.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Out(string collectionName)
        {
            return Append(new BsonDocument("$out", collectionName));
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
            Append(new BsonDocument("$project", _toBsonDocument(project)));

            return CopyToNew<TNewResult>(resultSerializer);
        }

        /// <summary>
        /// Redacts the specified redact.
        /// </summary>
        /// <param name="redact">The redact.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Redact(object redact)
        {
            return Append(new BsonDocument("$redact", _toBsonDocument(redact)));
        }

        /// <summary>
        /// Skips the specified skip.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Skip(int skip)
        {
            return Append(new BsonDocument("$skip", skip));
        }

        /// <summary>
        /// Sorts the specified sort.
        /// </summary>
        /// <param name="sort">The sort.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> Sort(object sort)
        {
            return Append(new BsonDocument("$sort", _toBsonDocument(sort)));
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
            Append(new BsonDocument("$unwind", fieldName));
            return CopyToNew<TNewResult>(resultSerializer);
        }

        /// <summary>
        /// Sets the use cursor option.
        /// </summary>
        /// <param name="useCursor">The use cursor.</param>
        /// <returns></returns>
        public AggregateFluent<TDocument, TResult> UseCursor(bool? useCursor)
        {
            _options.UseCursor = useCursor;
            return this;
        }

        /// <summary>
        /// To the asynchronous enumerable.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An asynchronous enumerable.</returns>
        public Task<IAsyncEnumerable<TResult>> ToAsyncEnumerable(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _collection.AggregateAsync(_options, timeout, cancellationToken);
        }

        /// <summary>
        /// To the list asynchronous.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of the result.</returns>
        public async Task<List<TResult>> ToListAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            timeout = timeout ?? _collection.Settings.OperationTimeout;
            var asyncEnumerable = await ToAsyncEnumerable(timeout, cancellationToken);
            return await asyncEnumerable.ToListAsync(timeout, cancellationToken);
        }

        private AggregateFluent<TDocument, TNewResult> CopyToNew<TNewResult>(IBsonSerializer<TNewResult> resultSerializer)
        {
            var newFluent = new AggregateFluent<TDocument, TNewResult>(_collection, _toBsonDocument);
            newFluent._options.AllowDiskUse = _options.AllowDiskUse;
            newFluent._options.BatchSize = _options.BatchSize;
            newFluent._options.MaxTime = _options.MaxTime;
            newFluent._options.Pipeline = _options.Pipeline;
            newFluent._options.ResultSerializer = resultSerializer ?? _collection.Settings.SerializerRegistry.GetSerializer<TNewResult>();
            newFluent._options.UseCursor = _options.UseCursor;

            return newFluent;
        }
    }
}
