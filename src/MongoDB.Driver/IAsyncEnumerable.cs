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
    /// <summary>
    /// An enumerable operating on an asynchronous stream.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public interface IAsyncEnumerable<out T>
    {
        /// <summary>
        /// Gets an async enumerator.
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }

    /// <summary>
    /// Extensions for <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    public static class IAsyncEnumerableExtensions
    {
        /// <summary>
        /// Creates a list asynchronousnly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asyncEnumerable">The asynchronous enumerable.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list.</returns>
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> asyncEnumerable, TimeSpan? timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(asyncEnumerable, "asyncEnumerable");

            var slidingTimeout = new SlidingTimeout(timeout ?? Timeout.InfiniteTimeSpan);
            var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
            var list = new List<T>();
            while(await asyncEnumerator.MoveNextAsync(slidingTimeout, cancellationToken))
            {
                list.Add(asyncEnumerator.Current);
            }
            return list;
        }
    }
}
