using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    internal class AsyncCursorHelper
    {
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
