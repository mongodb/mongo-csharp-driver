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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal class AggregateFluent<TCollectionDocument, TDocument> : IOrderedAggregateFluent<TDocument>
    {
        // fields
        private readonly IReadOnlyMongoCollection<TCollectionDocument> _collection;
        private readonly AggregateOptions<TDocument> _options;
        private readonly IList<BsonDocument> _pipeline;

        // constructors
        public AggregateFluent(IReadOnlyMongoCollection<TCollectionDocument> collection, IEnumerable<BsonDocument> pipeline, AggregateOptions<TDocument> options)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _options = Ensure.IsNotNull(options, "options");
        }

        // properties
        public AggregateOptions<TDocument> Options
        {
            get { return _options; }
        }

        public IList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
        }

        // methods
        public IAggregateFluent<TDocument> GeoNear(object geoNear)
        {
            return AppendStage(new BsonDocument("$geoNear", ConvertToBsonDocument(geoNear)));
        }

        public IAggregateFluent<TNewResult> Group<TNewResult>(Projection<TDocument, TNewResult> group)
        {
            var rendered = Ensure.IsNotNull(group, "group")
                .Render(_options.ResultSerializer, _collection.Settings.SerializerRegistry);

            return AppendStage<TNewResult>(new BsonDocument("$group", rendered.Document), rendered.Serializer);
        }

        public IAggregateFluent<TDocument> Limit(int limit)
        {
            return AppendStage(new BsonDocument("$limit", limit));
        }

        public IAggregateFluent<TDocument> Match(Filter<TDocument> filter)
        {
            var document = Ensure.IsNotNull(filter, "filter")
                .Render(_options.ResultSerializer, _collection.Settings.SerializerRegistry);

            return AppendStage(new BsonDocument("$match", document));
        }

        public Task<IAsyncCursor<TDocument>> OutAsync(string collectionName, CancellationToken cancellationToken)
        {
            AppendStage(new BsonDocument("$out", collectionName));
            return ToCursorAsync(cancellationToken);
        }

        public IAggregateFluent<TNewResult> Project<TNewResult>(Projection<TDocument, TNewResult> project)
        {
            var rendered = Ensure.IsNotNull(project, "project")
                .Render(_options.ResultSerializer, _collection.Settings.SerializerRegistry);

            return AppendStage<TNewResult>(new BsonDocument("$project", rendered.Document), rendered.Serializer);
        }

        public IAggregateFluent<TDocument> Redact(object redact)
        {
            return AppendStage(new BsonDocument("$redact", ConvertToBsonDocument(redact)));
        }

        public IAggregateFluent<TDocument> Skip(int skip)
        {
            return AppendStage(new BsonDocument("$skip", skip));
        }

        public IAggregateFluent<TDocument> Sort(Sort<TDocument> sort)
        {
            var document = Ensure.IsNotNull(sort, "sort")
                .Render(_options.ResultSerializer, _collection.Settings.SerializerRegistry);

            return AppendStage(new BsonDocument("$sort", document));
        }

        public IAggregateFluent<TResult> Unwind<TResult>(FieldName<TDocument> fieldName, IBsonSerializer<TResult> resultSerializer)
        {
            var renderedFieldName = Ensure.IsNotNull(fieldName, "fieldName")
                .Render(_options.ResultSerializer, _collection.Settings.SerializerRegistry);

            return AppendStage<TResult>(new BsonDocument("$unwind", "$" + renderedFieldName), resultSerializer);
        }

        public Task<IAsyncCursor<TDocument>> ToCursorAsync(CancellationToken cancellationToken)
        {
            return _collection.AggregateAsync(_pipeline, _options, cancellationToken);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("aggregate([");
            if (_pipeline.Count > 0)
            {
                foreach (var stage in _pipeline)
                {
                    sb.Append(ConvertToBsonDocument(stage));
                    sb.Append(", ");
                }
                sb.Remove(sb.Length - 2, 2);
            }
            sb.Append("])");
            return sb.ToString();
        }

        private IAggregateFluent<TDocument> AppendStage(BsonDocument stage)
        {
            _pipeline.Add(stage);
            return this;
        }

        private IAggregateFluent<TResult> AppendStage<TResult>(BsonDocument stage, IBsonSerializer<TResult> resultSerializer)
        {
            _pipeline.Add(stage);
            var newOptions = new AggregateOptions<TResult>
            {
                AllowDiskUse = _options.AllowDiskUse,
                BatchSize = _options.BatchSize,
                MaxTime = _options.MaxTime,
                ResultSerializer = resultSerializer ?? _collection.Settings.SerializerRegistry.GetSerializer<TResult>(),
                UseCursor = _options.UseCursor
            };

            return new AggregateFluent<TCollectionDocument, TResult>(_collection, _pipeline, newOptions);
        }

        private BsonDocument ConvertToBsonDocument(object document)
        {
            return BsonDocumentHelper.ToBsonDocument(_collection.Settings.SerializerRegistry, document);
        }
    }
}
