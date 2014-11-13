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
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(protocol, "protocol");
            using (var connection = await connectionSource.GetConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(
            this IWireProtocol<TResult> protocol,
            IReadBinding binding,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(protocol, "protocol");
            using (var connectionSource = await binding.GetReadConnectionSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var connection = await connectionSource.GetConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(
            this IWireProtocol<TResult> protocol,
            IWriteBinding binding,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(protocol, "protocol");
            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var connection = await connectionSource.GetConnectionAsync(cancellationToken).ConfigureAwait(false))
            {
                return await protocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
