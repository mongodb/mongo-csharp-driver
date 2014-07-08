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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol
{
    internal static class WireProtocolExtensionMethods
    {
        public static async Task<TResult> ExecuteAsync<TResult>(
            this IWireProtocol<TResult> protocol,
            IConnectionSource connectionSource,
            TimeSpan timeout = default(TimeSpan),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(protocol, "protocol");
            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connection = await connectionSource.GetConnectionAsync(slidingTimeout, cancellationToken))
            {
                return await protocol.ExecuteAsync(connection, slidingTimeout, cancellationToken);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(
            this IWireProtocol<TResult> protocol,
            IReadBinding binding,
            TimeSpan timeout = default(TimeSpan),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(protocol, "protocol");
            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetReadConnectionSourceAsync(slidingTimeout, cancellationToken))
            using (var connection = await connectionSource.GetConnectionAsync(slidingTimeout, cancellationToken))
            {
                return await protocol.ExecuteAsync(connection, slidingTimeout, cancellationToken);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(
            this IWireProtocol<TResult> protocol,
            IWriteBinding binding,
            TimeSpan timeout = default(TimeSpan),
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(protocol, "protocol");
            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(slidingTimeout, cancellationToken))
            using (var connection = await connectionSource.GetConnectionAsync(slidingTimeout, cancellationToken))
            {
                return await protocol.ExecuteAsync(connection, slidingTimeout, cancellationToken);
            }
        }
    }
}
