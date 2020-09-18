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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Linq3.Translators.QueryTranslators;

namespace MongoDB.Driver.Linq3
{
    public class MongoQuery<TDocument, TOutput> : IMongoQueryable<TOutput>
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

        public MongoQueryProvider<TDocument> Provider => _provider;

        IQueryProvider IQueryable.Provider => _provider;

        // public methods
        public IAsyncCursor<TOutput> Execute()
        {
            var executableQuery = QueryTranslator.TranslateMultiValuedQuery<TDocument, TOutput>(_provider, _expression);
            return executableQuery.Execute(_provider.Session, _provider.CancellationToken);
        }

        public Task<IAsyncCursor<TOutput>> ExecuteAsync()
        {
            var executableQuery = QueryTranslator.TranslateMultiValuedQuery<TDocument, TOutput>(_provider, _expression);
            return executableQuery.ExecuteAsync(_provider.Session, _provider.CancellationToken);
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

        public override string ToString()
        {
            try
            {
                var executableQuery = QueryTranslator.TranslateMultiValuedQuery<TDocument, TOutput>(_provider, _expression);
                return $"[{string.Join(", ", executableQuery.Stages.Select(s => s.ToJson()))}]";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
