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
    internal class AggregateFluent<TDocument, TResult> : IOrderedAggregateFluent<TDocument, TResult>
    {
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly AggregateOptions _options;
        private readonly IList<object> _pipeline;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        // constructors
        public AggregateFluent(IMongoCollection<TDocument> collection, IEnumerable<object> pipeline, AggregateOptions options, IBsonSerializer<TResult> resultSerializer)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _options = Ensure.IsNotNull(options, "options");
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
        }

        // properties
        public IMongoCollection<TDocument> Collection
        {
            get { return _collection; }
        }

        public AggregateOptions Options
        {
            get { return _options; }
        }

        public IList<object> Pipeline
        {
            get { return _pipeline; }
        }

        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        // methods
        public IAggregateFluent<TDocument, TResult> AppendStage(object stage)
        {
            _pipeline.Add(stage);
            return this;
        }

        public IAggregateFluent<TDocument, TResult> GeoNear(object geoNear)
        {
            return AppendStage(new BsonDocument("$geoNear", ConvertToBsonDocument(geoNear)));
        }

        public IAggregateFluent<TDocument, TResult> Group(object group)
        {
            return AppendStage(new BsonDocument("$group", ConvertToBsonDocument(group)));
        }

        public IAggregateFluent<TDocument, TResult> Limit(int limit)
        {
            return AppendStage(new BsonDocument("$limit", limit));
        }

        public IAggregateFluent<TDocument, TResult> Match(object filter)
        {
            return AppendStage(new BsonDocument("$match", ConvertToBsonDocument(filter)));
        }

        public IAggregateFluent<TDocument, TResult> Out(string collectionName)
        {
            return AppendStage(new BsonDocument("$out", collectionName));
        }

        public IAggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project)
        {
            return Project<TNewResult>(project, null);
        }

        public IAggregateFluent<TDocument, TNewResult> Project<TNewResult>(object project, IBsonSerializer<TNewResult> resultSerializer)
        {
            AppendStage(new BsonDocument("$project", ConvertToBsonDocument(project)));

            return CloneWithNewResultType<TNewResult>(resultSerializer);
        }

        public IAggregateFluent<TDocument, TResult> Redact(object redact)
        {
            return AppendStage(new BsonDocument("$redact", ConvertToBsonDocument(redact)));
        }

        public IAggregateFluent<TDocument, TResult> Skip(int skip)
        {
            return AppendStage(new BsonDocument("$skip", skip));
        }

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

        private IAggregateFluent<TDocument, TNewResult> CloneWithNewResultType<TNewResult>(IBsonSerializer<TNewResult> resultSerializer)
        {
            return new AggregateFluent<TDocument, TNewResult>(_collection, _pipeline, _options, resultSerializer);
        }

        private BsonDocument ConvertToBsonDocument(object document)
        {
            return BsonDocumentHelper.ConvertToBsonDocument(_collection.Settings.SerializerRegistry, document);
        }
    }
}
