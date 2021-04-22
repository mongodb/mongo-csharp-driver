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
using System.Threading;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver.Linq3
{
    internal static class IQueryableExtensions
    {
        public static IMongoQueryable<T> WithCancellationToken<T>(this IMongoQueryable<T> queryable, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(queryable, nameof(queryable));

            if (!(queryable.Provider is MongoQueryProvider provider))
            {
                throw new InvalidOperationException("WithCancellationToken can only be called when the QueryProvider is a MongoQueryProvider.");
            }

            provider = provider.WithCancellationToken(cancellationToken);
            return (IMongoQueryable<T>)provider.CreateQuery<T>(queryable.Expression);
        }

        public static IMongoQueryable<T> WithOptions<T>(this IMongoQueryable<T> queryable, AggregateOptions options)
        {
            Ensure.IsNotNull(queryable, nameof(queryable));

            if (!(queryable.Provider is MongoQueryProvider provider))
            {
                throw new InvalidOperationException("WithOptions can only be called when the QueryProvider is a MongoQueryProvider.");
            }

            provider = provider.WithOptions(options);
            return (IMongoQueryable<T>)provider.CreateQuery<T>(queryable.Expression);
        }

        public static IMongoQueryable<T> WithSession<T>(this IMongoQueryable<T> queryable, IClientSessionHandle session)
        {
            Ensure.IsNotNull(queryable, nameof(queryable));

            if (!(queryable.Provider is MongoQueryProvider provider))
            {
                throw new InvalidOperationException("WithCancellationToken can only be called when the QueryProvider is aa MongoQueryProvider.");
            }

            provider = provider.WithSession(session);
            return (IMongoQueryable<T>)provider.CreateQuery<T>(queryable.Expression);
        }
    }
}
