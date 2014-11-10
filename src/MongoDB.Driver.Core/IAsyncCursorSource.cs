/* Copyright 2010-2014 MongoDB Inc.
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
namespace MongoDB.Driver
{
    public interface IAsyncCursorSource<TDocument>
    {
        Task<IAsyncCursor<TDocument>> ToCursorAsync(CancellationToken cancellationToken);
    }

    public static class IAsyncCursorSourceExtensions
    {
        public static async Task ForEachAsync<TDocument>(this IAsyncCursorSource<TDocument> source, Func<TDocument, Task> processor, CancellationToken cancellationToken = default(CancellationToken))
        {
            await (await source.ToCursorAsync(cancellationToken)).ForEachAsync(processor, cancellationToken);
        }

        public static async Task ForEachAsync<TDocument>(this IAsyncCursorSource<TDocument> source, Func<TDocument, int, Task> processor, CancellationToken cancellationToken = default(CancellationToken))
        {
            await (await source.ToCursorAsync(cancellationToken)).ForEachAsync(processor, cancellationToken);
        }

        public static async Task<List<TDocument>> ToListAsync<TDocument>(this IAsyncCursorSource<TDocument> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await source.ToCursorAsync(cancellationToken)).ToListAsync(cancellationToken);
        }
    }
}
