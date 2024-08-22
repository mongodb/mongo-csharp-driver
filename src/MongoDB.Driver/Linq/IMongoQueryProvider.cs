/* Copyright 2015-present MongoDB Inc.
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

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// An implementation of <see cref="IQueryProvider" /> for MongoDB.
    /// </summary>
    public interface IMongoQueryProvider : IQueryProvider
    {
        /// <summary>
        /// Gets the most recently logged stages.
        /// </summary>
        BsonDocument[] LoggedStages { get; }

        /// <summary>
        /// Executes the strongly-typed query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// The internal IMongoQueryProvider interface.
    /// </summary>
    internal interface IMongoQueryProviderInternal : IMongoQueryProvider
    {
        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        CollectionNamespace CollectionNamespace { get; }

        /// <summary>
        /// Gets the pipeline input serializer (the DocumentSerializer for collection queries and NoPipelineInputSerializer for database queries).
        /// </summary>
        IBsonSerializer PipelineInputSerializer { get; }

        /// <summary>
        /// Gets the translation options.
        /// </summary>
        /// <returns>The translation options.</returns>
        ExpressionTranslationOptions GetTranslationOptions();
    }
}
