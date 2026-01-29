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

namespace MongoDB.Driver.Core.Bindings
{
    // TODO: CSOT: Make it public when CSOT will be ready for GA
    internal static class ICoreSessionExtensions
    {
        // TODO: Merge these extension methods in ICoreSession interface on major release
        public static void AbortTransaction(this ICoreSession session, AbortTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                session.AbortTransaction(cancellationToken);
                return;
            }

            ((ICoreSessionInternal)session).AbortTransaction(options, cancellationToken);
        }

        public static Task AbortTransactionAsync(this ICoreSession session, AbortTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                return session.AbortTransactionAsync(cancellationToken);
            }

            return ((ICoreSessionInternal)session).AbortTransactionAsync(options, cancellationToken);
        }

        public static void CommitTransaction(this ICoreSession session, CommitTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                session.CommitTransaction(cancellationToken);
                return;
            }

            ((ICoreSessionInternal)session).CommitTransaction(options, cancellationToken);
        }

        public static Task CommitTransactionAsync(this ICoreSession session, CommitTransactionOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null || session.Options.DefaultTransactionOptions?.Timeout == options.Timeout)
            {
                return session.CommitTransactionAsync(cancellationToken);
            }

            return ((ICoreSessionInternal)session).CommitTransactionAsync(options, cancellationToken);
        }
    }
}
