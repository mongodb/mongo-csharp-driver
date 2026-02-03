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
            var tokenBucket = context.Binding.TokenBucket;

            long? transactionNumber = null;
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

                    transactionNumber ??= AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

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
                    var innerException = ex is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : ex;
                    originalException ??= ex;

                    if (!ShouldRetry(operationContext, operation.WriteConcern, context, server, tokenBucket, innerException, attempt, context.Random, out var backoff))
                    {
                        throw originalException;
                    }

                    Thread.Sleep(backoff);
                    //TODO I should throw if backoff exceeeds timeout
                }

                if (server != null)
                {
                    deprioritizedServers ??= [];
                    deprioritizedServers.Add(server);
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
            var tokenBucket = context.Binding.TokenBucket;

            long? transactionNumber = null;
            while (true)
            {
                attempt++;
                operationContext.ThrowIfTimedOutOrCanceled();

                ServerDescription server = null;
                try
                {
                    //TODO This seems to be idempotent
                    await context.AcquireOrReplaceChannelAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                    ChannelPinningHelper.PinChannellIfRequired(context.ChannelSource, context.Channel, context.Binding.Session);
                    server = context.ChannelSource.ServerDescription;

                    //TODO This should be set only once
                    transactionNumber ??= AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

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

                if (server != null)
                {
                    deprioritizedServers ??= [];
                    deprioritizedServers.Add(server);
                }
            }
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
            var isAuthException = exception is MongoAuthenticationException;
            exception = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;

            bool isRetryableReadOrWrite;
            if (isAuthException)
            {
                //Auth operations are retried only according to retryable reads logic
                var isRetryableReadException = RetryabilityHelper.IsRetryableReadException(exception);
                isRetryableReadOrWrite = context.RetryRequested && !context.Binding.Session.IsInTransaction && isRetryableReadException;
            }
            else
            {
                var isRetryableWriteException = RetryabilityHelper.IsRetryableWriteException(exception);
                isRetryableReadOrWrite = AreRetriesAllowed(writeConcern, context, server) && isRetryableWriteException;
            }

            backoff = TimeSpan.Zero;
            var isRetryableException = RetryabilityHelper.IsRetryableException(exception);
            var isSystemOverloadedException = RetryabilityHelper.IsSystemOverloadedException(exception);


            var isBackpressureRetry = isSystemOverloadedException
                                      && isRetryableException;

            if (attempt > 1 && !isSystemOverloadedException)
            {
                tokenBucket.Deposit(1);
            }

            if (!isRetryableReadOrWrite && !isBackpressureRetry)
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
