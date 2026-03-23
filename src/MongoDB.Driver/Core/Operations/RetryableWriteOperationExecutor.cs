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
        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, IWriteBinding binding, bool retryRequested, bool canBeRetried, IMayUseSecondaryCriteria mayUseSecondary = null)
        {
            using var context = new RetryableWriteContext(binding, retryRequested, canBeRetried, mayUseSecondaryCriteria: mayUseSecondary);
            return Execute(operationContext, operation, context);
        }

        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var totalAttempts = 0;
            var operationExecutionAttempts = 0;
            Exception originalException = null;
            var tokenBucket = context.Binding.TokenBucket;
            var isEndTransactionOperation = operation is EndTransactionOperation;

            long? transactionNumber = null;

            while (true) // Circle breaking logic based on ShouldRetryOperation method, see the catch block below.
            {
                totalAttempts++;
                operationContext.ThrowIfTimedOutOrCanceled();
                ServerDescription server = null;

                try
                {
                    server = context.SelectServer(operationContext, deprioritizedServers);
                    context.AcquireChannel(operationContext);

                    transactionNumber ??= AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

                    operationExecutionAttempts++;

                    var operationResult = operation.ExecuteAttempt(operationContext, context, operationExecutionAttempts, transactionNumber);
                    if (tokenBucket != null)
                    {
                        var tokensToDeposit = RetryabilityHelper.OperationRetryBackpressureConstants.RetryTokenReturnRate;
                        if (totalAttempts > 1)
                        {
                            tokensToDeposit += 1;
                        }
                        tokenBucket.Deposit(tokensToDeposit);
                    }

                    if (context.Binding.Session.Id != null &&
                        context.Binding.Session.IsInTransaction)
                    {
                        context.Binding.Session.CurrentTransaction.HasCompletedCommand = true;
                    }

                    return operationResult;
                }
                catch (Exception ex)
                {
                    if (originalException == null || WritesWereAttempted(ex, context.Channel is not null))
                    {
                        originalException = ex;
                    }

                    if (!ShouldRetry(operationContext, context.Channel is null, server, operation.WriteConcern, context, tokenBucket, ex, totalAttempts, context.Random, isEndTransactionOperation, out var backoff))
                    {
                        throw originalException;
                    }

                    if (operation is EndTransactionOperation endTransactionOperation)
                    {
                        endTransactionOperation.OnRetry(context, ex);
                    }

                    Thread.Sleep(backoff);
                    deprioritizedServers = UpdateServerList(server, deprioritizedServers, ex);
                }
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, IWriteBinding binding, bool retryRequested, bool canBeRetried, IMayUseSecondaryCriteria mayUseSecondary = null)
        {
            using var context = new RetryableWriteContext(binding, retryRequested, canBeRetried, mayUseSecondaryCriteria: mayUseSecondary);
            return await ExecuteAsync(operationContext, operation, context).ConfigureAwait(false);
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            HashSet<ServerDescription> deprioritizedServers = null;
            var totalAttempts = 0;
            var operationExecutionAttempts = 0;
            Exception originalException = null;
            var tokenBucket = context.Binding.TokenBucket;
            var isEndTransactionOperation = operation is EndTransactionOperation;

            long? transactionNumber = null;
            while (true)
            {
                totalAttempts++;
                operationContext.ThrowIfTimedOutOrCanceled();
                ServerDescription server = null;

                try
                {
                    server = await context.SelectServerAsync(operationContext, deprioritizedServers).ConfigureAwait(false);
                    await context.AcquireChannelAsync(operationContext).ConfigureAwait(false);

                    operationExecutionAttempts++;
                    transactionNumber ??= AreRetriesAllowed(operation.WriteConcern, context, context.ChannelSource.ServerDescription) ? context.Binding.Session.AdvanceTransactionNumber() : null;

                    //TODO The "attempt" parameter is only used in RetryableWriteCommandOperationBase, and in particular in "CreateCommandPayloads", that uses it to decide what to include in the bulk write payloads
                    //It seems we should change this parameter to another one, as the attempt number is not correct in this new retryability. Also the current operationAttempt is not correct,
                    //probably the attempts should be counted only for the "retryable write" attempts?
                    var operationResult = await operation.ExecuteAttemptAsync(operationContext, context, operationExecutionAttempts, transactionNumber).ConfigureAwait(false);
                    if (tokenBucket != null)
                    {
                        var tokensToDeposit = RetryabilityHelper.OperationRetryBackpressureConstants.RetryTokenReturnRate;
                        if (totalAttempts > 1)
                        {
                            tokensToDeposit += 1;
                        }
                        tokenBucket.Deposit(tokensToDeposit);
                    }

                    if (context.Binding.Session.Id != null &&
                        context.Binding.Session.IsInTransaction)
                    {
                        context.Binding.Session.CurrentTransaction.HasCompletedCommand = true;
                    }

                    return operationResult;
                }
                catch (Exception ex)
                {
                    if (originalException == null || WritesWereAttempted(ex, context.Channel is not null))
                    {
                        originalException = ex;
                    }

                    if (!ShouldRetry(operationContext, context.Channel is null, server, operation.WriteConcern, context, tokenBucket, ex, totalAttempts, context.Random, isEndTransactionOperation, out var backoff))
                    {
                        throw originalException;
                    }

                    if (operation is EndTransactionOperation endTransactionOperation)
                    {
                        endTransactionOperation.OnRetry(context, ex);
                    }

                    await Task.Delay(backoff, operationContext.CancellationToken).ConfigureAwait(false);
                    deprioritizedServers = UpdateServerList(server, deprioritizedServers, ex);
                }
            }
        }

        // private static methods
        private static bool ShouldRetry(OperationContext operationContext,
            bool errorDuringChannelAcquisition,
            ServerDescription server,
            WriteConcern writeConcern,
            RetryableWriteContext context,
            TokenBucket tokenBucket,
            Exception exception,
            int attempt,
            IRandom random,
            bool isEndTransactionOperation,
            out TimeSpan backoff)
        {
            backoff = TimeSpan.Zero;

            if (!context.CanBeRetried)
            {
                return false;
            }

            bool isRetryableRead;
            if (errorDuringChannelAcquisition)
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
            // End-transaction operations (commit/abort) always retry on retryable write errors,
            // regardless of retryWrites setting or IsInTransaction state (per transactions spec).
            var isRetryableWrites = isRetryableWriteException &&
                (isEndTransactionOperation
                    ? IsOperationAcknowledged(writeConcern)
                    : AreRetriesAllowed(writeConcern, context, server));

            var isRetryableReadOrWrite = isRetryableRead || isRetryableWrites;

            var isRetryableException = RetryabilityHelper.IsRetryableException(exception);
            var isSystemOverloadedException = RetryabilityHelper.IsSystemOverloadedException(exception);

            var isBackpressureRetry = isSystemOverloadedException
                                      && isRetryableException;

            if (attempt > 1 && !isSystemOverloadedException && tokenBucket != null)
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

                var canConsumeToken = tokenBucket == null || tokenBucket.Consume(1);
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

        private static bool WritesWereAttempted(Exception exception, bool channelAcquisitionSuccessful)
            => channelAcquisitionSuccessful && !RetryabilityHelper.IsNoWritesPerformedException(exception);

        private static bool IsOperationAcknowledged(WriteConcern writeConcern)
            => writeConcern == null || // null means use server default write concern which implies acknowledged
               writeConcern.IsAcknowledged;

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
