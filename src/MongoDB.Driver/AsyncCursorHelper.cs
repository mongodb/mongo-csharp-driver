/* Copyright 2010-2015 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    internal static class AsyncCursorHelper
    {
        public async static Task<bool> AnyAsync<T>(Task<IAsyncCursor<T>> cursorTask, CancellationToken cancellationToken)
        {
            using (var cursor = await cursorTask.ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.Any();
                }

                return false;
            }
        }

        public static T First<T>(IAsyncCursor<T> cursor, CancellationToken cancellationToken)
        {
            using (cursor)
            {
                if (cursor.MoveNext(cancellationToken))
                {
                    return cursor.Current.First();
                }
                else
                {
                    throw new InvalidOperationException("The source sequence is empty.");
                }
            }
        }

        public async static Task<T> FirstAsync<T>(Task<IAsyncCursor<T>> cursorTask, CancellationToken cancellationToken)
        {
            using (var cursor = await cursorTask.ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.First();
                }
                else
                {
                    throw new InvalidOperationException("The source sequence is empty.");
                }
            }
        }

        public static T FirstOrDefault<T>(IAsyncCursor<T> cursor, CancellationToken cancellationToken)
        {
            using (cursor)
            {
                if (cursor.MoveNext(cancellationToken))
                {
                    return cursor.Current.FirstOrDefault();
                }
                else
                {
                    return default(T);
                }
            }
        }

        public async static Task<T> FirstOrDefaultAsync<T>(Task<IAsyncCursor<T>> cursorTask, CancellationToken cancellationToken)
        {
            using (var cursor = await cursorTask.ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.FirstOrDefault();
                }
                else
                {
                    return default(T);
                }
            }
        }

        public static T Single<T>(IAsyncCursor<T> cursor, CancellationToken cancellationToken)
        {
            using (cursor)
            {
                if (cursor.MoveNext(cancellationToken))
                {
                    return cursor.Current.Single();
                }
                else
                {
                    throw new InvalidOperationException("The source sequence is empty.");
                }
            }
        }

        public async static Task<T> SingleAsync<T>(Task<IAsyncCursor<T>> cursorTask, CancellationToken cancellationToken)
        {
            using (var cursor = await cursorTask.ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.Single();
                }
                else
                {
                    throw new InvalidOperationException("The source sequence is empty.");
                }
            }
        }

        public static T SingleOrDefault<T>(IAsyncCursor<T> cursor, CancellationToken cancellationToken)
        {
            using (cursor)
            {
                if (cursor.MoveNext(cancellationToken))
                {
                    return cursor.Current.SingleOrDefault();
                }
                else
                {
                    return default(T);
                }
            }
        }

        public async static Task<T> SingleOrDefaultAsync<T>(Task<IAsyncCursor<T>> cursorTask, CancellationToken cancellationToken)
        {
            using (var cursor = await cursorTask.ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    return cursor.Current.SingleOrDefault();
                }
                else
                {
                    return default(T);
                }
            }
        }
    }
}
