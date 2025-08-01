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
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    // TODO: CSOT: Make it public when CSOT will be ready for GA
    internal static class IClientSessionExtensions
    {
        // TODO: CSOT: Merge this extension methods in IClientSession interface on major release

        /// <summary>
        /// Aborts the transaction.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Abort transaction options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void AbortTransaction(this IClientSession session, AbortTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options?.Timeout == null || session.Options.DefaultTransactionOptions?.Timeout == options?.Timeout)
            {
                session.AbortTransaction(cancellationToken);
                return;
            }

            if (session is not ClientSessionHandle clientSessionHandle)
            {
                throw new InvalidOperationException("Cannot apply options on non ClientSessionHandle.");
            }

            clientSessionHandle.AbortTransaction(options, cancellationToken);
        }

        /// <summary>
        /// Aborts the transaction.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Abort transaction options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task AbortTransactionAsync(this IClientSession session, AbortTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options?.Timeout == null || session.Options.DefaultTransactionOptions?.Timeout == options?.Timeout)
            {
                return session.AbortTransactionAsync(cancellationToken);
            }

            if (session is not ClientSessionHandle clientSessionHandle)
            {
                throw new InvalidOperationException("Cannot apply options on non ClientSessionHandle.");
            }

            return clientSessionHandle.AbortTransactionAsync(options, cancellationToken);
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Commit transaction options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void CommitTransaction(this IClientSession session, CommitTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options?.Timeout == null || session.Options.DefaultTransactionOptions?.Timeout == options?.Timeout)
            {
                session.CommitTransaction(cancellationToken);
                return;
            }

            if (session is not ClientSessionHandle clientSessionHandle)
            {
                throw new InvalidOperationException("Cannot apply options on non ClientSessionHandle.");
            }

            clientSessionHandle.CommitTransaction(options, cancellationToken);
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Commit transaction options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task CommitTransactionAsync(this IClientSession session, CommitTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options?.Timeout == null || session.Options.DefaultTransactionOptions?.Timeout == options?.Timeout)
            {
                return session.CommitTransactionAsync(cancellationToken);
            }

            if (session is not ClientSessionHandle clientSessionHandle)
            {
                throw new InvalidOperationException("Cannot apply options on non ClientSessionHandle.");
            }

            return clientSessionHandle.CommitTransactionAsync(options, cancellationToken);
        }

        internal static ReadPreference GetEffectiveReadPreference(this IClientSession session, ReadPreference defaultReadPreference)
        {
            if (session.IsInTransaction)
            {
                var transactionReadPreference = session.WrappedCoreSession.CurrentTransaction.TransactionOptions?.ReadPreference;
                if (transactionReadPreference != null)
                {
                    return transactionReadPreference;
                }
            }

            return defaultReadPreference ?? ReadPreference.Primary;
        }
    }
}
