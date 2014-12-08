/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    public interface IAsyncCursor<TDocument> : IDisposable
    {
        IEnumerable<TDocument> Current { get; }

        Task<bool> MoveNextAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    public static class IAsyncCursorExtensions
    {
        public static Task ForEachAsync<TDocument>(this IAsyncCursor<TDocument> source, Func<TDocument, Task> processor, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ForEachAsync(source, (doc, _) => processor(doc), cancellationToken);
        }

        public static async Task ForEachAsync<TDocument>(this IAsyncCursor<TDocument> source, Func<TDocument, int, Task> processor, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(processor, "processor");

            // yes, we are taking ownership... assumption being that they've
            // exhausted the thing and don't need it anymore.
            using (source)
            {
                var index = 0;
                while (await source.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    foreach (var document in source.Current)
                    {
                        await processor(document, index++).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        public static Task ForEachAsync<TDocument>(this IAsyncCursor<TDocument> source, Action<TDocument> processor, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ForEachAsync(source, (doc, _) => processor(doc), cancellationToken);
        }

        public static async Task ForEachAsync<TDocument>(this IAsyncCursor<TDocument> source, Action<TDocument, int> processor, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");
            Ensure.IsNotNull(processor, "processor");

            // yes, we are taking ownership... assumption being that they've
            // exhausted the thing and don't need it anymore.
            using (source)
            {
                var index = 0;
                while (await source.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    foreach (var document in source.Current)
                    {
                        processor(document, index++);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        public static async Task<List<TDocument>> ToListAsync<TDocument>(this IAsyncCursor<TDocument> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(source, "source");

            var list = new List<TDocument>();

            // yes, we are taking ownership... assumption being that they've
            // exhausted the thing and don't need it anymore.
            using (source)
            {
                while (await source.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    list.AddRange(source.Current);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            return list;
        }
    }
}
