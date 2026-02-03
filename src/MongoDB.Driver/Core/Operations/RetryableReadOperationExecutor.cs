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
            var attempt = 0;
            Exception originalException = null;
            var tokenBucket = context.Binding.TokenBucket;

            while (true)
            {
                attempt++;
                operationContext.ThrowIfTimedOutOrCanceled();

                ServerDescription server = null;
                try
                {
                    context.AcquireOrReplaceChannel(operationContext, deprioritizedServers);
                    ChannelPinningHelper.PinChannellIfRequired(context.ChannelSource, context.Channel, context.Binding.Session);
                    server = context.ChannelSource.ServerDescription;

                    var operationResult = operation.ExecuteAttempt(operationContext, context, attempt, transactionNumber: null);
                    var tokensToDeposit = RetryabilityHelper.OperationRetryBackpressureConstants.RetryTokenReturnRate;
                    if (attempt > 1)
                    {
                        tokensToDeposit += 1;
                    }
                    tokenBucket.Deposit(tokensToDeposit);

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

                    if (!ShouldRetry(operationContext, context, tokenBucket, ex, attempt, context.Random, out var backoff))
                    {
                        throw originalException;
                    }

                    Thread.Sleep(backoff);
                }

                if (server != null)
                {
                    deprioritizedServers ??= [];
                    deprioritizedServers.Add(server);
                }
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableReadOperation<TResult> operation, RetryableReadContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            var tokenBucket = context.Binding.TokenBucket;

            while (true)
            {
                attempt++;
                operationContext.ThrowIfTimedOutOrCanceled();

                ServerDescription server = null;
                try
                {
                    await context.AcquireOrReplaceChannelAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                    ChannelPinningHelper.PinChannellIfRequired(context.ChannelSource, context.Channel, context.Binding.Session);
                    server = context.ChannelSource.ServerDescription;

                    var operationResult = await operation.ExecuteAttemptAsync(operationContext, context, attempt, transactionNumber: null).ConfigureAwait(false);
                    var tokensToDeposit = RetryabilityHelper.OperationRetryBackpressureConstants.RetryTokenReturnRate;
                    if (attempt > 1)
                    {
                        tokensToDeposit += 1;
                    }
                    tokenBucket.Deposit(tokensToDeposit);

                    //TODO Do we need this also here?
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

                    if (!ShouldRetry(operationContext, context, tokenBucket, ex, attempt, context.Random, out var backoff))
                    {
                        throw originalException;
                    }

                    await Task.Delay(backoff, operationContext.CancellationToken).ConfigureAwait(false);
                }

                if (server != null)
                {
                    deprioritizedServers ??= [];
                    deprioritizedServers.Add(server);
                }
            }
        }

        // private static methods
        private static bool ShouldRetry(OperationContext operationContext,
            RetryableReadContext context,
            TokenBucket tokenBucket,
            Exception exception,
            int attempt,
            IRandom random,
            out TimeSpan backoff)
        {
            //Authentication exceptions are wrapped inside MongoAuthenticationException, we need to unwrap them to be able to detect their retryability
            exception = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;

            backoff = TimeSpan.Zero;
            var isRetryableReadException = RetryabilityHelper.IsRetryableReadException(exception);
            var isRetryableException = RetryabilityHelper.IsRetryableException(exception);
            var isSystemOverloadedException = RetryabilityHelper.IsSystemOverloadedException(exception);

            var isRetryableRead = context.RetryRequested && !context.Binding.Session.IsInTransaction && isRetryableReadException;

            var isBackpressureRetry = isSystemOverloadedException
                                      && isRetryableException;

            if (attempt > 1 && !isSystemOverloadedException)
            {
                tokenBucket.Deposit(1);
            }

            if (!isRetryableRead && !isBackpressureRetry)
            {
                return false;
            }

            if (isSystemOverloadedException)
            {
                //TODO When the first command of a transaction fails with a backpressure error, we need to reset the transaction state
                //It needs to be put to "Starting" again. I've tried to cancel its transition to "InProgress" state in the first place, but that did not work well with the current implementation
                //(I was getting an end of error stream from the binary connection). This "reset" of the transaction state seems to work fine and is the same approach done by Python
                if (context.Binding.Session.Id != null
                    && context.Binding.Session.IsInTransaction
                    && context.Binding.Session.CurrentTransaction is { HasCompletedCommand: false } currentTransaction)
                {
                    currentTransaction.ResetState();
                }

                backoff = RetryabilityHelper.GetOperationRetryBackoffDelay(attempt, random);

                var canConsumeToken = tokenBucket.Consume(1);
                return canConsumeToken && attempt <= RetryabilityHelper.OperationRetryBackpressureConstants.MaxRetries;
            }

            //If a retryable write (not backpressure related), we retry "infinite" times (until timeout) with CSOT enabled, otherwise just once.
            return operationContext.IsRootContextTimeoutConfigured() || attempt < 2;
        }
    }
}
