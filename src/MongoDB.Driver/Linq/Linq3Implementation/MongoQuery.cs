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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation
{
    internal abstract class MongoQuery<TOutput>
    {
        public abstract IAsyncCursor<TOutput> Execute();
        public abstract Task<IAsyncCursor<TOutput>> ExecuteAsync();
    }

    internal class MongoQuery<TDocument, TOutput> : MongoQuery<TOutput>, IOrderedQueryable<TOutput>, IAsyncCursorSource<TOutput>, IMongoQueryableForwarder<TOutput>
    {
        // private fields
        private readonly Expression _expression;
        private readonly MongoQueryProvider<TDocument> _provider;

        // constructors
        public MongoQuery(MongoQueryProvider<TDocument> provider)
        {
            _provider = provider;
            _expression = Expression.Constant(this);
        }

        public MongoQuery(MongoQueryProvider<TDocument> provider, Expression expression)
        {
            _provider = provider;
            _expression = expression;
        }

        // public properties
        public Type ElementType => typeof(TOutput);

        public Expression Expression => _expression;

        public BsonDocument[] LoggedStages => _provider.LoggedStages;

        public IMongoQueryProvider Provider => _provider;

        IQueryProvider IQueryable.Provider => _provider;

        // public methods
        public override IAsyncCursor<TOutput> Execute()
        {
            var translationOptions = _provider.GetTranslationOptions();
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TOutput>(_provider, _expression, translationOptions);
            return _provider.Execute(executableQuery, CancellationToken.None);
        }

        public override Task<IAsyncCursor<TOutput>> ExecuteAsync()
        {
            var translationOptions = _provider.GetTranslationOptions();
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TOutput>(_provider, _expression, translationOptions);
            return _provider.ExecuteAsync(executableQuery, CancellationToken.None);
        }

        public IEnumerator<TOutput> GetEnumerator()
        {
            var cursor = Execute();
            return cursor.ToEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IAsyncCursor<TOutput> ToCursor(CancellationToken cancellationToken = default)
        {
            var translationOptions = _provider.GetTranslationOptions();
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TOutput>(_provider, _expression, translationOptions);
            return _provider.Execute(executableQuery, cancellationToken);
        }

        public Task<IAsyncCursor<TOutput>> ToCursorAsync(CancellationToken cancellationToken = default)
        {
            var translationOptions = _provider.GetTranslationOptions();
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TOutput>(_provider, _expression, translationOptions);
            return _provider.ExecuteAsync(executableQuery, cancellationToken);
        }

        public override string ToString()
        {
            try
            {
                var translationOptions = _provider.GetTranslationOptions();
                var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TOutput>(_provider, _expression, translationOptions);
                return executableQuery.ToString();
            }
            catch (ExpressionNotSupportedException ex)
            {
                return ex.Message;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        Task<bool> IMongoQueryableForwarder<TOutput>.AnyAsync(CancellationToken cancellationToken) => MongoQueryable.AnyAsync(this, cancellationToken);
        Task<TOutput> IMongoQueryableForwarder<TOutput>.FirstAsync(CancellationToken cancellationToken) => MongoQueryable.FirstAsync(this, cancellationToken);
        Task<TOutput> IMongoQueryableForwarder<TOutput>.FirstOrDefaultAsync(CancellationToken cancellationToken) => MongoQueryable.FirstOrDefaultAsync(this, cancellationToken);
        Task<TOutput> IMongoQueryableForwarder<TOutput>.SingleAsync(CancellationToken cancellationToken) => MongoQueryable.SingleAsync(this, cancellationToken);
        Task<TOutput> IMongoQueryableForwarder<TOutput>.SingleOrDefaultAsync(CancellationToken cancellationToken) => MongoQueryable.SingleOrDefaultAsync(this, cancellationToken);
    }
}
