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
    /// Implementation of IAggregateFluent
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class AggregateFluent<TDocument, TResult> : IOrderedAggregateFluent<TDocument, TResult>
    {
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly AggregateOptions _options;
        private readonly IList<object> _pipeline;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateFluent{TDocument, TResult}"/> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="options">The options.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public AggregateFluent(IMongoCollection<TDocument> collection, IEnumerable<object> pipeline, AggregateOptions options, IBsonSerializer<TResult> resultSerializer)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _options = Ensure.IsNotNull(options, "options");
            _resultSerializer = resultSerializer;
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
        /// Appends the stage.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TResult> AppendStage(object stage)
        {
            _pipeline.Add(stage);
            return this;
        }

        /// <summary>
        /// Geoes the near.
        /// </summary>
        /// <param name="geoNear">The geo near.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TResult> GeoNear(object geoNear)
        {
            return AppendStage(new BsonDocument("$geoNear", ConvertToBsonDocument(geoNear)));
        }

        /// <summary>
        /// Groups the specified group.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TNewResult> Group<TNewResult>(object group)
        {
            return Group<TNewResult>(group, null);
        }

        /// <summary>
        /// Groups the specified group.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="group">The group.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TNewResult> Group<TNewResult>(object group, IBsonSerializer<TNewResult> resultSerializer)
        {
            AppendStage(new BsonDocument("$group", ConvertToBsonDocument(group)));

            return CloneWithNewResultType<TNewResult>(resultSerializer);
        }

        /// <summary>
        /// Limits the specified limit.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TResult> Limit(int limit)
        {
            return AppendStage(new BsonDocument("$limit", limit));
        }

        /// <summary>
        /// Matches the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TResult> Match(object filter)
        {
            return AppendStage(new BsonDocument("$match", ConvertFilterToBsonDocument(filter)));
        }

        /// <summary>
        /// Outs the asynchronous.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task<IAsyncCursor<TResult>> OutAsync(string collectionName, CancellationToken cancellationToken)
        {
            AppendStage(new BsonDocument("$out", collectionName));
            return ToCursorAsync(cancellationToken);
        }

        /// <summary>
        /// Projects the specified project.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="project">The project.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project)
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
        public IAggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project, IBsonSerializer<TNewResult> resultSerializer)
        {
            AppendStage(new BsonDocument("$project", ConvertToBsonDocument(project)));

            return CloneWithNewResultType<TNewResult>(resultSerializer);
        }

        /// <summary>
        /// Redacts the specified redact.
        /// </summary>
        /// <param name="redact">The redact.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TResult> Redact(object redact)
        {
            return AppendStage(new BsonDocument("$redact", ConvertToBsonDocument(redact)));
        }

        /// <summary>
        /// Skips the specified skip.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TResult> Skip(int skip)
        {
            return AppendStage(new BsonDocument("$skip", skip));
        }

        /// <summary>
        /// Sorts the specified sort.
        /// </summary>
        /// <param name="sort">The sort.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TResult> Sort(object sort)
        {
            return AppendStage(new BsonDocument("$sort", ConvertToBsonDocument(sort)));
        }

        /// <summary>
        /// Unwinds the specified field name.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public IAggregateFluent<TDocument, TNewResult> Unwind<TNewResult>(string fieldName)
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
        public IAggregateFluent<TDocument, TNewResult> Unwind<TNewResult>(string fieldName, IBsonSerializer<TNewResult> resultSerializer)
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

        private IAggregateFluent<TDocument, TNewResult> CloneWithNewResultType<TNewResult>(IBsonSerializer<TNewResult> resultSerializer)
        {
            return new AggregateFluent<TDocument, TNewResult>(_collection, _pipeline, _options, resultSerializer);
        }

        private BsonDocument ConvertToBsonDocument(object document)
        {
            return BsonDocumentHelper.ToBsonDocument(_collection.Settings.SerializerRegistry, document);
        }

        private BsonDocument ConvertFilterToBsonDocument(object filter)
        {
            return BsonDocumentHelper.FilterToBsonDocument<TResult>(_collection.Settings.SerializerRegistry, filter);
        }
    }
}
