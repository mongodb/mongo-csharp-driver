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

namespace MongoDB.Driver.Core.Bindings
{
    // TODO: CSOT: Make it public when CSOT will be ready for GA
    internal static class ICoreSessionExtensions
    {
        // TODO: CSOT: Merge this extension methods in ICoreSession interface on major release
        /// <summary>
        /// Aborts the transaction.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Abort transaction options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void AbortTransaction(this ICoreSession session, AbortTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                session.AbortTransaction(cancellationToken);
                return;
            }

            if (session is CoreSession coreSession)
            {
                coreSession.AbortTransaction(options, cancellationToken);
                return;
            }

            if (session is WrappingCoreSession wrappingCoreSession)
            {
                wrappingCoreSession.AbortTransaction(options, cancellationToken);
                return;
            }

            throw new InvalidOperationException("Cannot apply options on non CoreSession.");
        }

        /// <summary>
        /// Aborts the transaction.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Abort transaction options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task AbortTransactionAsync(this ICoreSession session, AbortTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                return session.AbortTransactionAsync(cancellationToken);
            }

            if (session is CoreSession coreSession)
            {
                return coreSession.AbortTransactionAsync(options, cancellationToken);
            }

            if (session is WrappingCoreSession wrappingCoreSession)
            {
                return wrappingCoreSession.AbortTransactionAsync(options, cancellationToken);
            }

            throw new InvalidOperationException("Cannot apply options on non CoreSession.");
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Commit transaction options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void CommitTransaction(this ICoreSession session, CommitTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                session.CommitTransaction(cancellationToken);
                return;
            }

            if (session is CoreSession coreSession)
            {
                coreSession.CommitTransaction(options, cancellationToken);
                return;
            }

            if (session is WrappingCoreSession wrappingCoreSession)
            {
                wrappingCoreSession.CommitTransaction(options, cancellationToken);
                return;
            }

            throw new InvalidOperationException("Cannot apply options on non CoreSession.");
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Commit transaction options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task CommitTransactionAsync(this ICoreSession session, CommitTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                return session.CommitTransactionAsync(cancellationToken);
            }

            if (session is CoreSession coreSession)
            {
                return coreSession.CommitTransactionAsync(options, cancellationToken);
            }

            if (session is WrappingCoreSession wrappingCoreSession)
            {
                return wrappingCoreSession.CommitTransactionAsync(options, cancellationToken);
            }

            throw new InvalidOperationException("Cannot apply options on non CoreSession.");
        }
    }
}

