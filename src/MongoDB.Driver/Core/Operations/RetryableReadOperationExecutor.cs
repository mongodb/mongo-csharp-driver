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
            var totalAttempts = 0;
            Exception originalException = null;

            while (true) // Circle breaking logic based on ShouldRetryOperation method, see the catch block below.
            {
                totalAttempts++;
                operationContext.ThrowIfTimedOutOrCanceled();
                ServerDescription server = null;

                try
                {
                    server = context.DoServerSelection(operationContext, deprioritizedServers);
                    context.DoChannelAcquisition(operationContext);

                    return operation.ExecuteAttempt(operationContext, context, totalAttempts, transactionNumber: null);
                }
                catch (Exception ex)
                {
                    if (!ShouldRetryOperation(operationContext, context, ex, totalAttempts))
                    {
                        throw originalException ?? ex;
                    }

                    originalException ??= ex;
                    deprioritizedServers = UpdateServerList(server, deprioritizedServers, ex);
                }
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableReadOperation<TResult> operation, RetryableReadContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var totalAttempts = 0;
            Exception originalException = null;

            while (true) // Circle breaking logic based on ShouldRetryOperation method, see the catch block below.
            {
                totalAttempts++;
                operationContext.ThrowIfTimedOutOrCanceled();
                ServerDescription server = null;

                try
                {
                    server = await context.DoServerSelectionAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                    await context.DoChannelAcquisitionAsync(operationContext).ConfigureAwait(false);

                    return await operation.ExecuteAttemptAsync(operationContext, context, totalAttempts, transactionNumber: null).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!ShouldRetryOperation(operationContext, context, ex, totalAttempts))
                    {
                        throw originalException ?? ex;
                    }

                    originalException ??= ex;
                    deprioritizedServers = UpdateServerList(server, deprioritizedServers, ex);
                }
            }
        }

        // private static methods
        private static bool ShouldRetryOperation(OperationContext operationContext, RetryableReadContext context, Exception exception, int totalAttempts)
        {
            exception = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;

            if (!context.RetryRequested || context.Binding.Session.IsInTransaction)
            {
                return false;
            }

            if (!RetryabilityHelper.IsRetryableReadException(exception))
            {
                return false;
            }

            return operationContext.IsRootContextTimeoutConfigured() || totalAttempts < 2;
        }

        private static HashSet<ServerDescription> UpdateServerList(ServerDescription server, HashSet<ServerDescription> deprioritizedServers, Exception ex)
        {
            if (server != null && (server.Type == ServerType.ShardRouter ||
                                   (ex is MongoException mongoException && mongoException.HasErrorLabel("SystemOverloadedError"))))
            {
                deprioritizedServers ??= [];
                deprioritizedServers.Add(server);
            }

            return deprioritizedServers;
        }
    }
}
