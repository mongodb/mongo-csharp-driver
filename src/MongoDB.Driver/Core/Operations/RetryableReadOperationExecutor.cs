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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
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
                    server = context.SelectServer(operationContext, deprioritizedServers);
                    context.AcquireChannel(operationContext);

                    var operationResult = operation.ExecuteAttempt(operationContext, context, totalAttempts, transactionNumber: null);

                    if (context.Binding.Session.Id != null &&
                        context.Binding.Session.IsInTransaction)
                    {
                        context.Binding.Session.CurrentTransaction.HasCompletedCommand = true;
                    }

                    return operationResult;
                }
                catch (Exception ex)
                {
                    originalException ??= ex;

                    if (!ShouldRetry(operationContext, context, operation.IsOperationRetryable, ex, totalAttempts, context.Random, out var backoff))
                    {
                        throw originalException;
                    }

                    // We bail early if the backoff would exceed the CSOT deadline.
                    var remaining = operationContext.RemainingTimeout;
                    if (remaining != Timeout.InfiniteTimeSpan && operationContext.IsRootContextTimeoutConfigured() && remaining < backoff)
                    {
                        throw originalException;
                    }

                    Thread.Sleep(backoff);
                    deprioritizedServers = UpdateServerList(server, deprioritizedServers, ex, context.EnableOverloadRetargeting);
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
                    server = await context.SelectServerAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                    await context.AcquireChannelAsync(operationContext).ConfigureAwait(false);

                    var operationResult = await operation.ExecuteAttemptAsync(operationContext, context, totalAttempts, transactionNumber: null).ConfigureAwait(false);

                    if (context.Binding.Session.Id != null &&
                        context.Binding.Session.IsInTransaction)
                    {
                        context.Binding.Session.CurrentTransaction.HasCompletedCommand = true;
                    }

                    return operationResult;
                }
                catch (Exception ex)
                {
                    originalException ??= ex;

                    if (!ShouldRetry(operationContext, context, operation.IsOperationRetryable, ex, totalAttempts, context.Random, out var backoff))
                    {
                        throw originalException;
                    }

                    await Task.Delay(backoff, operationContext.CancellationToken).ConfigureAwait(false);
                    deprioritizedServers = UpdateServerList(server, deprioritizedServers, ex, context.EnableOverloadRetargeting);
                }
            }
        }

        // private static methods
        private static bool ShouldRetry(OperationContext operationContext,
            RetryableReadContext context,
            bool isOperationRetryable,
            Exception exception,
            int attempt,
            IRandom random,
            out TimeSpan backoff)
        {
            backoff = TimeSpan.Zero;

            // Top-level gate: if the user disabled retries, nothing retries (including backpressure).
            if (!context.RetryRequested)
            {
                return false;
            }

            //Authentication exceptions are wrapped inside MongoAuthenticationException, we need to unwrap them to be able to detect their retryability
            exception = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;

            var isRetryableReadException = RetryabilityHelper.IsRetryableReadException(exception);
            var isRetryableException = RetryabilityHelper.IsRetryableException(exception);
            var isSystemOverloadedException = RetryabilityHelper.IsSystemOverloadedException(exception);

            var isRetryableRead = isOperationRetryable && !context.Binding.Session.IsInTransaction && isRetryableReadException;

            var isBackpressureRetry = isSystemOverloadedException
                                      && isRetryableException;

            if (!isRetryableRead && !isBackpressureRetry)
            {
                return false;
            }

            if (isSystemOverloadedException)
            {
                // If the first command in a transaction was rejected due to overload, reset to Starting so the retry re-sends startTransaction:true.
                if (context.Binding.Session.Id != null
                    && context.Binding.Session.IsInTransaction
                    && context.Binding.Session.CurrentTransaction is { HasCompletedCommand: false } currentTransaction)
                {
                    currentTransaction.ResetState();
                }

                backoff = RetryabilityHelper.GetOperationRetryBackoffDelay(attempt, random);

                return attempt <= context.MaxAdaptiveRetries;
            }

            //If a retryable read (not backpressure related), we retry "infinite" times (until timeout) with CSOT enabled, otherwise just once.
            return operationContext.IsRootContextTimeoutConfigured() || attempt < 2;
        }

        private static HashSet<ServerDescription> UpdateServerList(ServerDescription server, HashSet<ServerDescription> deprioritizedServers, Exception ex, bool enableOverloadRetargeting)
        {
            if (server != null && (server.Type == ServerType.ShardRouter ||
                                   (enableOverloadRetargeting && ex is MongoException mongoException && mongoException.HasErrorLabel("SystemOverloadedError"))))
            {
                deprioritizedServers ??= [];
                deprioritizedServers.Add(server);
            }

            return deprioritizedServers;
        }
    }
}
