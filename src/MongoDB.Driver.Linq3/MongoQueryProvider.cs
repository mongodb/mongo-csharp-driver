/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Translators.QueryTranslators;

namespace MongoDB.Driver.Linq3
{
    public abstract class MongoQueryProvider : IQueryProvider
    {
        // protected fields
        protected readonly CancellationToken _cancellationToken;
        protected readonly AggregateOptions _options;
        protected readonly IClientSessionHandle _session;

        // constructors
        protected MongoQueryProvider(
            IClientSessionHandle session, 
            AggregateOptions options,
            CancellationToken cancellationToken)
        {
            _session = session;
            _options = options;
            _cancellationToken = cancellationToken;
        }

        // public properties
        public CancellationToken CancellationToken => _cancellationToken;
        public abstract IBsonSerializer DocumentSerializer { get; }
        public AggregateOptions Options => _options;
        public IClientSessionHandle Session => _session;

        // public methods
        public abstract IQueryable CreateQuery(Expression expression);
        public abstract IQueryable<TElement> CreateQuery<TElement>(Expression expression);
        public abstract object Execute(Expression expression);
        public abstract TResult Execute<TResult>(Expression expression);
        public abstract Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);
        public MongoQueryProvider WithCancellationToken(CancellationToken cancellationToken) => WithCancellationTokenGeneric(cancellationToken);
        public MongoQueryProvider WithOptions(AggregateOptions options) => WithOptionsGeneric(options);
        public MongoQueryProvider WithSession(IClientSessionHandle session) => WithSessionGeneric(session);

        // protected methods
        protected abstract MongoQueryProvider WithCancellationTokenGeneric(CancellationToken cancellationToken);
        protected abstract MongoQueryProvider WithOptionsGeneric(AggregateOptions options);
        protected abstract MongoQueryProvider WithSessionGeneric(IClientSessionHandle session);
    }

    public sealed class MongoQueryProvider<TDocument> : MongoQueryProvider
    {
        // private fields
        private readonly IMongoCollection<TDocument> _collection;

        // constructors
        public MongoQueryProvider(
            IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            AggregateOptions options,
            CancellationToken cancellationToken)
            : base(session, options, cancellationToken)
        {
            _collection = collection;
        }

        // public properties
        public IMongoCollection<TDocument> Collection => _collection;
        public override IBsonSerializer DocumentSerializer => _collection.DocumentSerializer;

        // public methods
        public override IQueryable CreateQuery(Expression expression)
        {
            throw new NotImplementedException();
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
            var executableQuery = QueryTranslator.TranslateScalarQuery<TDocument, TResult>(this, expression);
            return executableQuery.Execute(_session, _cancellationToken);
        }

        public override Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var executableQuery = QueryTranslator.TranslateScalarQuery<TDocument, TResult>(this, expression);
            return executableQuery.ExecuteAsync(_session, cancellationToken);
        }

        public new MongoQueryProvider<TDocument> WithCancellationToken(CancellationToken cancellationToken)
        {
            return new MongoQueryProvider<TDocument>(_collection, _session, _options, cancellationToken);
        }

        public new MongoQueryProvider<TDocument> WithOptions(AggregateOptions options)
        {
            return new MongoQueryProvider<TDocument>(_collection, _session, options, _cancellationToken);
        }

        public new MongoQueryProvider<TDocument> WithSession(IClientSessionHandle session)
        {
            return new MongoQueryProvider<TDocument>(_collection, session, _options, _cancellationToken);
        }

        // protected methods
        protected override MongoQueryProvider WithCancellationTokenGeneric(CancellationToken cancellationToken)
        {
            return WithCancellationToken(cancellationToken);
        }

        protected override MongoQueryProvider WithOptionsGeneric(AggregateOptions options)
        {
            return WithOptions(options);
        }

        protected override MongoQueryProvider WithSessionGeneric(IClientSessionHandle session)
        {
            return WithSession(session);
        }
    }
}
