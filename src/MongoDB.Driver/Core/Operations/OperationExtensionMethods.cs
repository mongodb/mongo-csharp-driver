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

using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    internal static class OperationExtensionMethods
    {
        public static TResult Execute<TResult>(
            this IReadOperation<TResult> operation,
            OperationContext operationContext,
            IChannelSourceHandle channelSource,
            ReadPreference readPreference,
            ICoreSessionHandle session)
        {
            Ensure.IsNotNull(operation, nameof(operation));
            using (var readBinding = new ChannelSourceReadWriteBinding(channelSource.Fork(), readPreference, session.Fork()))
            {
                return operation.Execute(operationContext, readBinding);
            }
        }

        public static TResult Execute<TResult>(
            this IWriteOperation<TResult> operation,
            OperationContext operationContext,
            IChannelSourceHandle channelSource,
            ICoreSessionHandle session)
        {
            Ensure.IsNotNull(operation, nameof(operation));
            using (var writeBinding = new ChannelSourceReadWriteBinding(channelSource.Fork(), ReadPreference.Primary, session.Fork()))
            {
                return operation.Execute(operationContext, writeBinding);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(
            this IReadOperation<TResult> operation,
            OperationContext operationContext,
            IChannelSourceHandle channelSource,
            ReadPreference readPreference,
            ICoreSessionHandle session)
        {
            Ensure.IsNotNull(operation, nameof(operation));
            using (var readBinding = new ChannelSourceReadWriteBinding(channelSource.Fork(), readPreference, session.Fork()))
            {
                return await operation.ExecuteAsync(operationContext, readBinding).ConfigureAwait(false);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(
            this IWriteOperation<TResult> operation,
            OperationContext operationContext,
            IChannelSourceHandle channelSource,
            ICoreSessionHandle session)
        {
            Ensure.IsNotNull(operation, nameof(operation));
            using (var writeBinding = new ChannelSourceReadWriteBinding(channelSource.Fork(), ReadPreference.Primary, session.Fork()))
            {
                return await operation.ExecuteAsync(operationContext, writeBinding).ConfigureAwait(false);
            }
        }
    }
}
