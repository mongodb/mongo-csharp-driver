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
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> WithCancellationToken<T>(this IQueryable<T> queryable, CancellationToken cancellationToken)
        {
            Throw.IfNull(queryable, nameof(queryable));

            if (!(queryable.Provider is MongoQueryProvider provider))
            {
                throw new InvalidOperationException("WithCancellationToken can only be called when the QueryProvider is a MongoQueryProvider.");
            }

            provider = provider.WithCancellationToken(cancellationToken);
            return provider.CreateQuery<T>(queryable.Expression);
        }

        public static IQueryable<T> WithOptions<T>(this IQueryable<T> queryable, AggregateOptions options)
        {
            Throw.IfNull(queryable, nameof(queryable));

            if (!(queryable.Provider is MongoQueryProvider provider))
            {
                throw new InvalidOperationException("WithOptions can only be called when the QueryProvider is a MongoQueryProvider.");
            }

            provider = provider.WithOptions(options);
            return provider.CreateQuery<T>(queryable.Expression);
        }

        public static IQueryable<T> WithSession<T>(this IQueryable<T> queryable, IClientSessionHandle session)
        {
            Throw.IfNull(queryable, nameof(queryable));

            if (!(queryable.Provider is MongoQueryProvider provider))
            {
                throw new InvalidOperationException("WithCancellationToken can only be called when the QueryProvider is aa MongoQueryProvider.");
            }

            provider = provider.WithSession(session);
            return provider.CreateQuery<T>(queryable.Expression);
        }
    }
}
