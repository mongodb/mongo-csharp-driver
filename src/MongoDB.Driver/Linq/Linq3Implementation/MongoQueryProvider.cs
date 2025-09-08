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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Linq3Implementation
{
    internal abstract class MongoQueryProvider : IMongoQueryProviderInternal
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
        public abstract CollectionNamespace CollectionNamespace { get; }
        public abstract BsonDocument[] LoggedStages { get; }
        public AggregateOptions Options => _options;
        public abstract IBsonSerializer PipelineInputSerializer { get; }
        public IClientSessionHandle Session => _session;

        // public methods
        public abstract IQueryable CreateQuery(Expression expression);
        public abstract IQueryable<TElement> CreateQuery<TElement>(Expression expression);
        public abstract object Execute(Expression expression);
        public abstract TResult Execute<TResult>(Expression expression);
        public abstract Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);
        public abstract ExpressionTranslationOptions GetTranslationOptions();
    }

    internal sealed class MongoQueryProvider<TDocument> : MongoQueryProvider
    {
        // private fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly IMongoDatabase _database;
        private ExecutableQuery<TDocument> _executedQuery;
        private readonly IBsonSerializer _pipelineInputSerializer;
        private readonly IBsonSerializationDomain _serializationDomain;

        // constructors
        public MongoQueryProvider(
            IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            AggregateOptions options)
            : base(session, options)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _pipelineInputSerializer = collection.DocumentSerializer;
            _serializationDomain = collection.Settings?.SerializationDomain ?? BsonSerializer.DefaultSerializationDomain;
        }

        public MongoQueryProvider(
            IMongoDatabase database,
            IClientSessionHandle session,
            AggregateOptions options)
            : base(session, options)
        {
            _database = Ensure.IsNotNull(database, nameof(database));
            _pipelineInputSerializer = NoPipelineInputSerializer.Instance;
            _serializationDomain = _database.Settings.SerializationDomain;
        }

        internal MongoQueryProvider(
            IBsonSerializer pipelineInputSerializer,
            IClientSessionHandle session,
            AggregateOptions options,
            IBsonSerializationDomain serializationDomain)
            : base(session, options)
        {
            _pipelineInputSerializer = Ensure.IsNotNull(pipelineInputSerializer, nameof(pipelineInputSerializer));
            _serializationDomain = Ensure.IsNotNull(serializationDomain, nameof(serializationDomain));
        }

        // public properties
        public IMongoCollection<TDocument> Collection => _collection;
        public override CollectionNamespace CollectionNamespace => _collection == null ? null : _collection.CollectionNamespace;
        public IMongoDatabase Database => _database;
        public override BsonDocument[] LoggedStages => _executedQuery?.LoggedStages;
        public override IBsonSerializer PipelineInputSerializer => _pipelineInputSerializer;
        public IBsonSerializationDomain SerializationDomain => _serializationDomain;

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

        public override object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public override TResult Execute<TResult>(Expression expression)
        {
            var translationOptions = GetTranslationOptions();
            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TDocument, TResult>(this, expression, translationOptions);
            return Execute(executableQuery, CancellationToken.None);
        }

        public TResult Execute<TResult>(ExecutableQuery<TDocument, TResult> executableQuery, CancellationToken cancellationToken)
        {
            _executedQuery = executableQuery;
            return executableQuery.Execute(_session, cancellationToken);
        }

        public override Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var translationOptions = GetTranslationOptions();
            var executableQuery = ExpressionToExecutableQueryTranslator.TranslateScalar<TDocument, TResult>(this, expression, translationOptions);
            return ExecuteAsync(executableQuery, cancellationToken);
        }

        public Task<TResult> ExecuteAsync<TResult>(ExecutableQuery<TDocument, TResult> executableQuery, CancellationToken cancellationToken)
        {
            _executedQuery = executableQuery;
            return executableQuery.ExecuteAsync(_session, cancellationToken);
        }

        public override ExpressionTranslationOptions GetTranslationOptions()
        {
            var translationOptions = _options?.TranslationOptions;
            var database = _database ?? _collection?.Database;
            return translationOptions.AddMissingOptionsFrom(database?.Client.Settings.TranslationOptions);
        }
    }
}
