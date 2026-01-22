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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Operations
{
    internal static class RetryableWriteOperationExecutor
    {
        // public static methods
        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, IWriteBinding binding, bool retryRequested)
        {
            using var context = RetryableWriteContext.Create(operationContext, binding, retryRequested);
            return Execute(operationContext, operation, context);
        }

        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            var maxAttempts = 1;
            var tokenBucket = context.ChannelSource.Server.TokenBucket;

            long? transactionNumber = AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

            while (true)
            {
                attempt++;
                operationContext.ThrowIfTimedOutOrCanceled();
                var server = context.ChannelSource.ServerDescription;
                bool isSystemOverloaded;

                try
                {
                    var operationResult = operation.ExecuteAttempt(operationContext, context, attempt, transactionNumber);
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
                    //TODO Which exception do we want to raise in case of backpressure retries exhausted?
                    originalException ??= ex;

                    var isRetryableWrite = IsRetryableWrite(operationContext, operation.WriteConcern, context, server, ex, attempt);
                    var isErrorRetryable = RetryabilityHelper.IsRetryableError(ex);
                    isSystemOverloaded = RetryabilityHelper.IsSystemOverloadedError(ex);

                    var isRetryable = isRetryableWrite || (isErrorRetryable && isSystemOverloaded);

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

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, IWriteBinding binding, bool retryRequested)
        {
            using var context = await RetryableWriteContext.CreateAsync(operationContext, binding, retryRequested).ConfigureAwait(false);
            return await ExecuteAsync(operationContext, operation, context).ConfigureAwait(false);
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            var maxAttempts = 1;
            var tokenBucket = context.ChannelSource.Server.TokenBucket;

            long? transactionNumber = AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

            while (true)
            {
                attempt++;
                operationContext.ThrowIfTimedOutOrCanceled();
                var server = context.ChannelSource.ServerDescription;
                bool isSystemOverloaded;

                try
                {
                    var operationResult = await operation.ExecuteAttemptAsync(operationContext, context, attempt, transactionNumber).ConfigureAwait(false);
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

                    var isRetryableWrite = IsRetryableWrite(operationContext, operation.WriteConcern, context, server, ex, attempt);
                    var isErrorRetryable = RetryabilityHelper.IsRetryableError(ex);
                    isSystemOverloaded = RetryabilityHelper.IsSystemOverloadedError(ex);

                    var isRetryable = isRetryableWrite || (isErrorRetryable && isSystemOverloaded);

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

                deprioritizedServers ??= new HashSet<ServerDescription>();
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

        public static bool ShouldConnectionAcquireBeRetried(OperationContext operationContext, RetryableWriteContext context, ServerDescription server, Exception exception, int attempt)
        {
            if (!DoesContextAllowRetries(context, server))
            {
                return false;
            }

            var innerException = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;
            // According the spec error during handshake should be handle according to RetryableReads logic
            if (!RetryabilityHelper.IsRetryableReadException(innerException))
            {
                return false;
            }

            return operationContext.IsRootContextTimeoutConfigured() || attempt < 2;
        }

        // private static methods
        private static bool IsRetryableWrite(OperationContext operationContext, WriteConcern writeConcern, RetryableWriteContext context, ServerDescription server, Exception exception, int attempt)
        {
            if (!AreRetriesAllowed(writeConcern, context, server))
            {
                return false;
            }

            if (!RetryabilityHelper.IsRetryableWriteException(exception))
            {
                return false;
            }

            return operationContext.IsRootContextTimeoutConfigured() || attempt < 2;
        }

        private static bool AreRetriesAllowed(WriteConcern writeConcern, RetryableWriteContext context, ServerDescription server)
            => IsOperationAcknowledged(writeConcern) && DoesContextAllowRetries(context, server);

        private static bool AreRetryableWritesSupported(ServerDescription serverDescription)
        {
            return serverDescription.Type == ServerType.LoadBalanced ||
                (serverDescription.LogicalSessionTimeout != null && serverDescription.Type != ServerType.Standalone);
        }

        private static bool DoesContextAllowRetries(RetryableWriteContext context, ServerDescription server)
            => context.RetryRequested &&
               AreRetryableWritesSupported(server) &&
               context.Binding.Session.Id != null &&
               !context.Binding.Session.IsInTransaction;

        private static bool IsOperationAcknowledged(WriteConcern writeConcern)
            => writeConcern == null || // null means use server default write concern which implies acknowledged
               writeConcern.IsAcknowledged;

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
