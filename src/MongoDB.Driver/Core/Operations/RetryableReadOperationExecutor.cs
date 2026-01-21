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
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Operations
{
    internal static class RetryableReadOperationExecutor
    {
        // public static methods
        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableReadOperation<TResult> operation, RetryableReadContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            var maxAttempts = 1;
            var tokenBucket = context.ChannelSource.Server.TokenBucket;

            while (true) // Circle breaking logic based on ShouldRetryOperation method, see the catch block below.
            {
                attempt++;
                operationContext.ThrowIfTimedOutOrCanceled();
                var server = context.ChannelSource.ServerDescription;
                bool isSystemOverloaded;

                try
                {
                    var operationResult = operation.ExecuteAttempt(operationContext, context, attempt, transactionNumber: null);
                    var tokensToDeposit = RetryabilityHelper.OperationRetryBackpressureConstants.RetryTokenReturnRate;
                    if (attempt > 1)
                    {
                        tokensToDeposit += 1;
                    }
                    tokenBucket.Deposit(tokensToDeposit);
                    return operationResult;
                }
                catch (Exception ex)
                {
                    originalException ??= ex;

                    var isRetryableRead = IsRetryableRead(operationContext, context, ex, attempt);
                    var isErrorRetryable = RetryabilityHelper.IsRetryableError(ex);
                    isSystemOverloaded = RetryabilityHelper.IsSystemOverloadedError(ex);

                    var isRetryable = isRetryableRead || (isErrorRetryable && isSystemOverloaded);

                    if (attempt > 1 && !isSystemOverloaded)
                    {
                        tokenBucket.Deposit(1);
                    }

                    if (!isRetryable)
                    {
                        throw originalException;
                    }

                    if (isSystemOverloaded)
                    {
                        maxAttempts = RetryabilityHelper.OperationRetryBackpressureConstants.MaxRetries;
                    }

                    if (attempt > maxAttempts)
                    {
                        throw originalException;
                    }
                }

                deprioritizedServers ??= [];
                deprioritizedServers.Add(server);

                if (isSystemOverloaded)
                {
                    var backoff = RetryabilityHelper.GetOperationRetryBackoffDelay(attempt);

                    if (IsTimedOut(operationContext, backoff) || !tokenBucket.Consume(1))
                    {
                        throw originalException;
                    }

                    Thread.Sleep(backoff);
                }

                //TODO What to do with this?
                try
                {
                    context.AcquireOrReplaceChannel(operationContext, deprioritizedServers);
                }
                catch
                {
                    throw originalException;
                }
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableReadOperation<TResult> operation, RetryableReadContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            var maxAttempts = 1;
            var tokenBucket = context.ChannelSource.Server.TokenBucket;

            while (true) // Circle breaking logic based on ShouldRetryOperation method, see the catch block below.
            {
                attempt++;
                operationContext.ThrowIfTimedOutOrCanceled();
                var server = context.ChannelSource.ServerDescription;
                bool isSystemOverloaded;

                try
                {
                    var operationResult = await operation.ExecuteAttemptAsync(operationContext, context, attempt, transactionNumber: null).ConfigureAwait(false);
                    var tokensToDeposit = RetryabilityHelper.OperationRetryBackpressureConstants.RetryTokenReturnRate;
                    if (attempt > 1)
                    {
                        tokensToDeposit += 1;
                    }
                    tokenBucket.Deposit(tokensToDeposit);
                    return operationResult;
                }
                catch (Exception ex)
                {
                    originalException ??= ex;

                    var isRetryableRead = IsRetryableRead(operationContext, context, ex, attempt);
                    var isErrorRetryable = RetryabilityHelper.IsRetryableError(ex);
                    isSystemOverloaded = RetryabilityHelper.IsSystemOverloadedError(ex);

                    var isRetryable = isRetryableRead || (isErrorRetryable && isSystemOverloaded);

                    if (attempt > 1 && !isSystemOverloaded)
                    {
                        tokenBucket.Deposit(1);
                    }

                    if (!isRetryable)
                    {
                        throw originalException;
                    }

                    if (isSystemOverloaded)
                    {
                        maxAttempts = RetryabilityHelper.OperationRetryBackpressureConstants.MaxRetries;
                    }

                    if (attempt > maxAttempts)
                    {
                        throw originalException;
                    }

                }

                deprioritizedServers ??= [];
                deprioritizedServers.Add(server);

                if (isSystemOverloaded)
                {
                    var backoff = RetryabilityHelper.GetOperationRetryBackoffDelay(attempt);

                    if (IsTimedOut(operationContext, backoff) || !tokenBucket.Consume(1))
                    {
                        throw originalException;
                    }

                    await Task.Delay(backoff).ConfigureAwait(false);
                }

                //TODO What to do with this?
                try
                {
                    await context.AcquireOrReplaceChannelAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                }
                catch
                {
                    throw originalException;
                }
            }
        }

        //TODO What to do with this....?
        public static bool ShouldConnectionAcquireBeRetried(OperationContext operationContext, RetryableReadContext context, Exception exception, int attempt)
        {
            var innerException = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;
            return IsRetryableRead(operationContext, context, innerException, attempt);
        }

        // private static methods
        private static bool IsRetryableRead(OperationContext operationContext, RetryableReadContext context, Exception exception, int attempt)
        {
            if (!context.RetryRequested || context.Binding.Session.IsInTransaction)
            {
                return false;
            }

            if (!RetryabilityHelper.IsRetryableReadException(exception))
            {
                return false;
            }

            return operationContext.IsRootContextTimeoutConfigured() || attempt < 2;
        }

        private static bool IsTimedOut(OperationContext operationContext, TimeSpan delay = default)
        {
            if (operationContext.Timeout.HasValue)
            {
                return operationContext.Elapsed + delay >= operationContext.Timeout;
            }

            return false;
        }
    }
}
