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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation
{
    internal class QueryableExecutionModel3<TDocument, TResult> : QueryableExecutionModel
    {
        // private fields
        private readonly ExecutableQuery<TDocument, TResult> _executableQuery;

        // constructors
        public QueryableExecutionModel3(ExecutableQuery<TDocument, TResult> executableQuery)
        {
            _executableQuery = Ensure.IsNotNull(executableQuery, nameof(executableQuery));
        }

        // public properties
        public override Type OutputType => typeof(TResult); // TResult is the equivalent of QueryableExecutionModel's OutputType

        // methods
        internal override object Execute<TInput>(IMongoCollection<TInput> collection, IClientSessionHandle session, AggregateOptions options)
        {
            var modifiedExecutableQuery = _executableQuery.WithCollection((IMongoCollection<TDocument>)collection).WithOptions(options);
            return modifiedExecutableQuery.Execute(session, CancellationToken.None);
        }

        internal override Task ExecuteAsync<TInput>(IMongoCollection<TInput> collection, IClientSessionHandle session, AggregateOptions options, CancellationToken cancellationToken)
        {
            var modifiedExecutableQuery = _executableQuery.WithCollection((IMongoCollection<TDocument>)collection).WithOptions(options);
            return modifiedExecutableQuery.ExecuteAsync(session, cancellationToken);
        }
    }
}
