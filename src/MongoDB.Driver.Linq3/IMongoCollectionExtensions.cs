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

using System.Linq;
using System.Threading;

namespace MongoDB.Driver.Linq3
{
    internal static class IMongoCollectionExtensions
    {
        public static IQueryable<TDocument> AsQueryable3<TDocument>(
            this IMongoCollection<TDocument> collection,
            IClientSessionHandle session = null,
            AggregateOptions options = null,
            CancellationToken cancellationToken = default)
        {
            var provider = new MongoQueryProvider<TDocument>(collection, session, options, cancellationToken);
            return new MongoQuery<TDocument, TDocument>(provider);
        }
    }
}
