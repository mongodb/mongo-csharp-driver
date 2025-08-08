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

using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    // TODO: CSOT: Make it public when CSOT will be ready for GA
    internal static class IClientSessionExtensions
    {
        // TODO: Merge these extension methods in IClientSession interface on major release
        public static void AbortTransaction(this IClientSession session, AbortTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options?.Timeout == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                session.AbortTransaction(cancellationToken);
                return;
            }

            ((IClientSessionInternal)session).AbortTransaction(options, cancellationToken);
        }

        public static Task AbortTransactionAsync(this IClientSession session, AbortTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options?.Timeout == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                return session.AbortTransactionAsync(cancellationToken);
            }

            return ((IClientSessionInternal)session).AbortTransactionAsync(options, cancellationToken);
        }

        public static void CommitTransaction(this IClientSession session, CommitTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options?.Timeout == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                session.CommitTransaction(cancellationToken);
                return;
            }

            ((IClientSessionInternal)session).CommitTransaction(options, cancellationToken);
        }

        public static Task CommitTransactionAsync(this IClientSession session, CommitTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options?.Timeout == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                return session.CommitTransactionAsync(cancellationToken);
            }

            return ((IClientSessionInternal)session).CommitTransactionAsync(options, cancellationToken);
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
