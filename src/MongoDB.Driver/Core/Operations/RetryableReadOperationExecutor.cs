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
    //TODO We could decide to merge the retryableRead and retryableWrite executors into a single executor (not in this PR)
    internal static class RetryableReadOperationExecutor
    {
        const int basePowerBackoff = 2;
        const double initialBackoff = 0.1;
        const int maxBackoff = 10;
        const int maxRetries = 5;
        const double retryTokenReturnRate = 0.1;

        //TODO This is taken from Alex's PR, we'll remove them after that is merged
        private static int GetRetryDelayMs(IRandom random, int attempt, double backoffBase, double backoffInitial, int backoffMax)
        {
            Ensure.IsNotNull(random, nameof(random));
            Ensure.IsGreaterThanZero(attempt, nameof(attempt));
            Ensure.IsGreaterThanZero(backoffBase, nameof(backoffBase));
            Ensure.IsGreaterThanZero(backoffInitial, nameof(backoffInitial));
            Ensure.IsGreaterThan(backoffMax, backoffInitial, nameof(backoffMax));

            var j = random.NextDouble();
            return (int)(j * Math.Min(backoffMax, backoffInitial * Math.Pow(backoffBase, attempt - 1)));
        }

        // public static methods
        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableReadOperation<TResult> operation, RetryableReadContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;  // TODO This is to keep consistency with the withTransaction work
            Exception originalException = null;
            var maxAttempts = 1;  //TODO Do we have CSOT implemented here? According to the spec here "if CSOT, then math.inf, otherwise 1"

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
                    var tokensToDeposit = retryTokenReturnRate;
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
                        maxAttempts = maxRetries;
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
                    var backoff = GetBackoffDelay(attempt);

                    if (IsTimedOut(operationContext, backoff) || !tokenBucket.Consume(1))
                    {
                        throw originalException;
                    }

                    Thread.Sleep(backoff);
                }

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
            var attempt = 1;
            Exception originalException = null;

            while (true) // Circle breaking logic based on ShouldRetryOperation method, see the catch block below.
            {
                operationContext.ThrowIfTimedOutOrCanceled();
                var server = context.ChannelSource.ServerDescription;
                try
                {
                    return await operation.ExecuteAttemptAsync(operationContext, context, attempt, transactionNumber: null).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!IsRetryableRead(operationContext, context, ex, attempt))
                    {
                        throw originalException ?? ex;
                    }

                    originalException ??= ex;
                }

                deprioritizedServers ??= new HashSet<ServerDescription>();
                deprioritizedServers.Add(server);

                try
                {
                    await context.AcquireOrReplaceChannelAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                }
                catch
                {
                    throw originalException;
                }

                attempt++;
            }
        }

        //TODO What to do with this....?
        public static bool ShouldConnectionAcquireBeRetried(OperationContext operationContext, RetryableReadContext context, Exception exception, int attempt)
        {
            var innerException = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;
            return IsRetryableRead(operationContext, context, innerException, attempt);
        }

        // TODO Do we ever check that the server is at least version 3.6 (wire version 6) to be sure the server supports retryable reads? It seems we don't, maybe checking if we get a retryable read error is enough?
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

            //TODO Do we need to keep this first check here?
            return operationContext.IsRootContextTimeoutConfigured() || attempt < 2;
        }

        // TODO Move in right place and add the correct logic
        private static TimeSpan GetBackoffDelay(int attempt)
        {
            TimeSpan.FromMilliseconds(GetRetryDelayMs(DefaultRandom.Instance, attempt, basePowerBackoff, initialBackoff, maxBackoff));
        }

        // TODO Move in right place
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
