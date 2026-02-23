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
            using var context = new RetryableWriteContext(binding, retryRequested, mayUseSecondaryCriteria: mayUseSecondary);
            return Execute(operationContext, operation, context);
        }

        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var totalAttempts = 0;
            var operationExecutionAttempts = 0;
            Exception originalException = null;
            var tokenBucket = context.Binding.TokenBucket;

            long? transactionNumber = null;
            while (true)
            {
                totalAttempts++;
                operationContext.ThrowIfTimedOutOrCanceled();
                try
                {
                    context.AcquireOrReplaceChannel(operationContext, deprioritizedServers);
                    transactionNumber ??= AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

                    operationExecutionAttempts++;

                    var operationResult = operation.ExecuteAttempt(operationContext, context, operationExecutionAttempts, transactionNumber);
                    var tokensToDeposit = RetryabilityHelper.OperationRetryBackpressureConstants.RetryTokenReturnRate;
                    if (totalAttempts > 1)
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

                    if (!ShouldRetry(operationContext, operation.WriteConcern, context, tokenBucket, ex, totalAttempts, context.Random, out var backoff))
                    {
                        throw originalException;
                    }

                    Thread.Sleep(backoff);
                    //TODO I should throw if backoff exceeeds timeout
                }

                deprioritizedServers ??= new HashSet<ServerDescription>();
                deprioritizedServers.Add(context.LastAcquiredServer);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, IWriteBinding binding, bool retryRequested, IMayUseSecondaryCriteria mayUseSecondary = null)
        {
            using var context = new RetryableWriteContext(binding, retryRequested, mayUseSecondaryCriteria: mayUseSecondary);
            return await ExecuteAsync(operationContext, operation, context).ConfigureAwait(false);
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var totalAttempts = 0;
            var operationExecutionAttempts = 0;
            Exception originalException = null;
            var tokenBucket = context.Binding.TokenBucket;

            long? transactionNumber = null;
            while (true)
            {
                totalAttempts++;
                operationContext.ThrowIfTimedOutOrCanceled();
                try
                {
                    await context.AcquireOrReplaceChannelAsync(operationContext, deprioritizedServers).ConfigureAwait(false);

                    operationExecutionAttempts++;
                    transactionNumber ??= AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

                    //TODO The "attempt" parameter is only used in RetryableWriteCommandOperationBase, and in particular in "CreateCommandPayloads", that uses it to decide what to include in the bulk write payloads
                    //It seems we should change this parameter to another one, as the attempt number is not correct in this new retryability. Also the current operationAttempt is not correct,
                    //probably the attempts should be counted only for the "retryable write" attempts?
                    var operationResult = await operation.ExecuteAttemptAsync(operationContext, context, operationExecutionAttempts, transactionNumber).ConfigureAwait(false);
                    var tokensToDeposit = RetryabilityHelper.OperationRetryBackpressureConstants.RetryTokenReturnRate;
                    if (totalAttempts > 1)
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

                    if (!ShouldRetry(operationContext, operation.WriteConcern, context, tokenBucket, ex, totalAttempts, context.Random, out var backoff))
                    {
                        throw originalException;
                    }

                    await Task.Delay(backoff, operationContext.CancellationToken).ConfigureAwait(false);
                }

                deprioritizedServers ??= new HashSet<ServerDescription>();
                deprioritizedServers.Add(context.LastAcquiredServer);
            }
        }

        // private static methods
        private static bool ShouldRetry(OperationContext operationContext,
            WriteConcern writeConcern,
            RetryableWriteContext context,
            TokenBucket tokenBucket,
            Exception exception,
            int attempt,
            IRandom random,
            out TimeSpan backoff)
        {
            backoff = TimeSpan.Zero;

            bool isRetryableRead;
            if (context.ErrorDuringLastChannelAcquisition)
            {
                //Exceptions from channel acquisition could be wrapped in a MongoAuthenticationException, in which case we need to look at the inner exception to decide if it's retryable or not
                var exceptionToAnalyze = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;

                var isRetryableReadException = RetryabilityHelper.IsRetryableReadException(exceptionToAnalyze);
                isRetryableRead = context.RetryRequested && !context.Binding.Session.IsInTransaction && isRetryableReadException;
            }
            else
            {
                isRetryableRead = false;
            }

            var isRetryableWriteException = RetryabilityHelper.IsRetryableWriteException(exception);
            var isRetryableWrites = AreRetriesAllowed(writeConcern, context, context.ChannelSource.ServerDescription) && isRetryableWriteException;

            var isRetryableReadOrWrite = isRetryableRead || isRetryableWrites;

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
