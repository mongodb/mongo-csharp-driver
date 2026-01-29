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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Operations
{
    internal static class RetryableWriteOperationExecutor
    {
        // public static methods
        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, IWriteBinding binding, bool retryRequested, IMayUseSecondaryCriteria mayUseSecondary = null)
        {
            using var context = RetryableWriteContext.Create(operationContext, binding, retryRequested, mayUseSecondary);
            return Execute(operationContext, operation, context);
        }

        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            var tokenBucket = context.Binding.Session.GetTokenBucket();

            long? transactionNumber = AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

            while (true)
            {
                attempt++;
                operationContext.ThrowIfTimedOutOrCanceled();
                var server = context.ChannelSource.ServerDescription;

                try
                {
                    var operationResult = operation.ExecuteAttempt(operationContext, context, attempt, transactionNumber);
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

                    if (!ShouldRetry(operationContext, operation.WriteConcern, context, server, tokenBucket, ex, attempt, context.Random, out var backoff))
                    {
                        throw originalException;
                    }

                    Thread.Sleep(backoff);
                    //TODO I should throw if backoff exceeeds timeout
                }

                deprioritizedServers ??= [];
                deprioritizedServers.Add(server);

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

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, IWriteBinding binding, bool retryRequested, IMayUseSecondaryCriteria mayUseSecondary = null)
        {
            using var context = await RetryableWriteContext.CreateAsync(operationContext, binding, retryRequested, mayUseSecondary).ConfigureAwait(false);
            return await ExecuteAsync(operationContext, operation, context).ConfigureAwait(false);
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var attempt = 0;
            Exception originalException = null;
            var tokenBucket = context.Binding.Session.GetTokenBucket();

            long? transactionNumber = AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

            while (true)
            {
                attempt++;
                operationContext.ThrowIfTimedOutOrCanceled();
                var server = context.ChannelSource.ServerDescription;

                try
                {
                    var operationResult = await operation.ExecuteAttemptAsync(operationContext, context, attempt, transactionNumber).ConfigureAwait(false);
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

                    if (!ShouldRetry(operationContext, operation.WriteConcern, context, server, tokenBucket, ex, attempt, context.Random, out var backoff))
                    {
                        throw originalException;
                    }

                    await Task.Delay(backoff, operationContext.CancellationToken).ConfigureAwait(false);
                }

                deprioritizedServers ??= [];
                deprioritizedServers.Add(server);

                //TODO Should this be retried as well?
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
        private static bool ShouldRetry(OperationContext operationContext,
            WriteConcern writeConcern,
            RetryableWriteContext context,
            ServerDescription server,
            TokenBucket tokenBucket,
            Exception exception,
            int attempt,
            IRandom random,
            out TimeSpan backoff)
        {
            backoff = TimeSpan.Zero;
            var isRetryableWriteException = RetryabilityHelper.IsRetryableWriteException(exception);
            var isRetryableException = RetryabilityHelper.IsRetryableException(exception);
            var isSystemOverloadedException = RetryabilityHelper.IsSystemOverloadedException(exception);

            var isRetryableWrite = AreRetriesAllowed(writeConcern, context, server) && isRetryableWriteException;

            var isBackpressureRetry = isSystemOverloadedException
                                      && isRetryableException;

            if (attempt > 1 && !isSystemOverloadedException)
            {
                tokenBucket.Deposit(1);
            }

            if (!isRetryableWrite && !isBackpressureRetry)
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
    }
}
