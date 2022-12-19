﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Linq3Implementation
{
    internal abstract class MongoQueryProvider : IMongoQueryProvider
    {
        // protected fields
        protected readonly AggregateOptions _options;
        protected readonly IClientSessionHandle _session;

        // constructors
        protected MongoQueryProvider(
            IClientSessionHandle session, 
            AggregateOptions options)
        {
            _session = session;
            _options = options;
        }

        // public properties
        public abstract IBsonSerializer CollectionDocumentSerializer { get; }
        public abstract CollectionNamespace CollectionNamespace { get; }
        public AggregateOptions Options => _options;
        public IClientSessionHandle Session => _session;

        // public methods
        public abstract IQueryable CreateQuery(Expression expression);
        public abstract IQueryable<TElement> CreateQuery<TElement>(Expression expression);
        public abstract QueryableExecutionModel GetExecutionModel(Expression expression);
        public abstract object Execute(Expression expression);
        public abstract TResult Execute<TResult>(Expression expression);
        public abstract Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);
        public abstract BsonDocument[] GetMostRecentPipelineStages();
    }

    internal sealed class MongoQueryProvider<TDocument> : MongoQueryProvider
    {
        // private fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly IMongoDatabase _database;
        private ExecutableQuery<TDocument> _mostRecentExecutableQuery;

        // constructors
        public MongoQueryProvider(
            IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            AggregateOptions options)
            : base(session, options)
        {
            _collection = collection;
        }

        public MongoQueryProvider(
            IMongoDatabase database,
            IClientSessionHandle session,
            AggregateOptions options)
            : base(session, options)
        {
            _database = database;
        }

        // public properties
        public IMongoCollection<TDocument> Collection => _collection;
        public override CollectionNamespace CollectionNamespace => _collection.CollectionNamespace;
        public override IBsonSerializer CollectionDocumentSerializer => _collection.DocumentSerializer;
        public IMongoDatabase Database => _database;

        // public methods
        public override IQueryable CreateQuery(Expression expression)
        {
            var outputType = expression.Type.GetSequenceElementType();
            var queryType = typeof(MongoQuery<,>).MakeGenericType(typeof(TDocument), outputType);
            return (IQueryable)Activator.CreateInstance(queryType, new object[] { this, expression });
        }

        public override IQueryable<TOutput> CreateQuery<TOutput>(Expression expression)
        {
            return new MongoQuery<TDocument, TOutput>(this, expression);
        }

        public override QueryableExecutionModel GetExecutionModel(Expression expression)
        {
            throw new NotSupportedException("This method is only supported in LINQ2 and will be removed in the future.");
        }

        public override object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public override TResult Execute<TResult>(Expression expression)
        {
            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TDocument, TResult>(this, expression);
            _mostRecentExecutableQuery = executableQuery;
            return executableQuery.Execute(_session, CancellationToken.None);
        }

        public override Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TDocument, TResult>(this, expression);
            _mostRecentExecutableQuery = executableQuery;
            return executableQuery.ExecuteAsync(_session, cancellationToken);
        }

        public override BsonDocument[] GetMostRecentPipelineStages()
        {
            var pipeline = _mostRecentExecutableQuery.Pipeline;
            var renderedPipeline = (BsonArray)pipeline.Render();
            return renderedPipeline.Cast<BsonDocument>().ToArray();
        }
    }
}
