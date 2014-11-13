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

namespace MongoDB.Driver.Core.Operations
{
    internal static class OperationExtensionMethods
    {
        public static async Task<TResult> ExecuteAsync<TResult>(
            this IReadOperation<TResult> operation,
            IConnectionSourceHandle connectionSource,
            ReadPreference readPreference,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(operation, "operation");
            using (var readBinding = new ConnectionSourceReadWriteBinding(connectionSource.Fork(), readPreference))
            {
                return await operation.ExecuteAsync(readBinding, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(
            this IWriteOperation<TResult> operation,
            IConnectionSourceHandle connectionSource,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(operation, "operation");
            using (var writeBinding = new ConnectionSourceReadWriteBinding(connectionSource.Fork(), ReadPreference.Primary))
            {
                return await operation.ExecuteAsync(writeBinding, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
