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
        private readonly AggregateOptions<TResult> _options;
        private readonly IList<object> _pipeline;

        // constructors
        public AggregateFluent(IMongoCollection<TDocument> collection, IEnumerable<object> pipeline, AggregateOptions<TResult> options)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _options = Ensure.IsNotNull(options, "options");
        }

        // properties
        public IMongoCollection<TDocument> Collection
        {
            get { return _collection; }
        }

        public AggregateOptions<TResult> Options
        {
            get { return _options; }
        }

        public IList<object> Pipeline
        {
            get { return _pipeline; }
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

        public IAggregateFluent<TDocument, TNewResult> Group<TNewResult>(object group)
        {
            return Group<TNewResult>(group, null);
        }

        public IAggregateFluent<TDocument, TNewResult> Group<TNewResult>(object group, IBsonSerializer<TNewResult> resultSerializer)
        {
            AppendStage(new BsonDocument("$group", ConvertToBsonDocument(group)));

            return CloneWithNewResultType<TNewResult>(resultSerializer);
        }

        public IAggregateFluent<TDocument, TResult> Limit(int limit)
        {
            return AppendStage(new BsonDocument("$limit", limit));
        }

        public IAggregateFluent<TDocument, TResult> Match(object filter)
        {
            return AppendStage(new BsonDocument("$match", ConvertFilterToBsonDocument(filter)));
        }

        public Task<IAsyncCursor<TResult>> OutAsync(string collectionName, CancellationToken cancellationToken)
        {
            AppendStage(new BsonDocument("$out", collectionName));
            return ToCursorAsync(cancellationToken);
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

        public IAggregateFluent<TDocument, TNewResult> Unwind<TNewResult>(string fieldName)
        {
            return Unwind<TNewResult>(fieldName, null);
        }

        public IAggregateFluent<TDocument, TNewResult> Unwind<TNewResult>(string fieldName, IBsonSerializer<TNewResult> resultSerializer)
        {
            AppendStage(new BsonDocument("$unwind", fieldName));
            return CloneWithNewResultType<TNewResult>(resultSerializer);
        }

        public Task<IAsyncCursor<TResult>> ToCursorAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _collection.AggregateAsync(_pipeline, _options, cancellationToken);
        }

        private IAggregateFluent<TDocument, TNewResult> CloneWithNewResultType<TNewResult>(IBsonSerializer<TNewResult> resultSerializer)
        {
            var newOptions = new AggregateOptions<TNewResult>
            {
                AllowDiskUse = _options.AllowDiskUse,
                BatchSize = _options.BatchSize,
                MaxTime = _options.MaxTime,
                ResultSerializer = resultSerializer,
                UseCursor = _options.UseCursor
            };
            return new AggregateFluent<TDocument, TNewResult>(_collection, _pipeline, newOptions);
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
