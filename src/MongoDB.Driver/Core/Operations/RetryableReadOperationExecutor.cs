/* Copyright 2019-present MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Operations
{
    internal static class RetryableReadOperationExecutor
    {
        // public static methods
        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableReadOperation<TResult> operation, RetryableReadContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 1;
            Exception originalException = null;

            while (true)
            {
                operationContext.ThrowIfTimedOutOrCanceled();

                try
                {
                    return operation.ExecuteAttempt(operationContext, context, attempt, transactionNumber: null);
                }
                catch (Exception ex)
                {
                    if (!ShouldRetryOperation(operationContext, context, ex, attempt))
                    {
                        throw originalException ?? ex;
                    }

                    originalException ??= ex;
                }

                deprioritizedServers ??= new HashSet<ServerDescription>();
                deprioritizedServers.Add(context.ChannelSource.ServerDescription);

                try
                {
                    context.Initialize(operationContext, deprioritizedServers);
                }
                catch
                {
                    throw originalException;
                }

                attempt++;
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableReadOperation<TResult> operation, RetryableReadContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 1;
            Exception originalException = null;

            while (true)
            {
                operationContext.ThrowIfTimedOutOrCanceled();

                try
                {
                    return await operation.ExecuteAttemptAsync(operationContext, context, attempt, transactionNumber: null).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!ShouldRetryOperation(operationContext, context, ex, attempt))
                    {
                        throw originalException ?? ex;
                    }

                    originalException ??= ex;
                }

                deprioritizedServers ??= new HashSet<ServerDescription>();
                deprioritizedServers.Add(context.ChannelSource.ServerDescription);

                try
                {
                    await context.InitializeAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                }
                catch
                {
                    throw originalException;
                }

                attempt++;
            }
        }

        // private static methods
        private static bool AreRetriesAllowed(RetryableReadContext context)
            => context.RetryRequested && !context.Binding.Session.IsInTransaction;

        public static bool ShouldRetryOperation(OperationContext operationContext, RetryableReadContext context, Exception exception, int attempt)
        {
            if (!AreRetriesAllowed(context))
            {
                return false;
            }

            if (!RetryabilityHelper.IsRetryableReadException(exception))
            {
                return false;
            }

            return operationContext.HasOperationTimeout() || attempt < 2;
        }
    }
}
