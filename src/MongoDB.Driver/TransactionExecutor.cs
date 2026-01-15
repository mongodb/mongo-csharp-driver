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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    internal static class TransactionExecutor
    {
        // constants
        private const string TransientTransactionErrorLabel = "TransientTransactionError";
        private const string UnknownTransactionCommitResultLabel = "UnknownTransactionCommitResult";
        private static readonly TimeSpan __transactionTimeout = TimeSpan.FromSeconds(120);

        public static TResult ExecuteWithRetries<TResult>(
            IClientSessionHandle clientSession,
            Func<IClientSessionHandle, CancellationToken, TResult> callback,
            TransactionOptions transactionOptions,
            IClock clock,
            IRandom random,
            CancellationToken cancellationToken)
        {
            var attempt = 0;
            var transactionTimeout = transactionOptions?.Timeout ?? clientSession.Options.DefaultTransactionOptions?.Timeout;
            using var operationContext = new OperationContext(clock, transactionTimeout, cancellationToken);

            while (true)
            {
                attempt++;
                clientSession.StartTransaction(transactionOptions);
                clientSession.WrappedCoreSession.CurrentTransaction.OperationContext = operationContext;

                try
                {
                    var result = callback(clientSession, cancellationToken);
                    // Transaction could be completed by user's code inside the callback, skipping commit in such case.
                    if (IsTransactionInStartingOrInProgressState(clientSession))
                    {
                        CommitWithRetries(operationContext, clientSession, cancellationToken);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    if(ShouldAbortTransaction(operationContext, clientSession, out var abortOptions))
                    {
                        clientSession.AbortTransaction(abortOptions, cancellationToken);
                    }

                    if (!ShouldRetryTransaction(operationContext, ex, random, attempt, out var delay))
                    {
                        throw;
                    }

                    Thread.Sleep(delay);
                }
            }
        }

        public static async Task<TResult> ExecuteWithRetriesAsync<TResult>(
            IClientSessionHandle clientSession,
            Func<IClientSessionHandle, CancellationToken, Task<TResult>> callbackAsync,
            TransactionOptions transactionOptions,
            IClock clock,
            IRandom random,
            CancellationToken cancellationToken)
        {
            var attempt = 0;
            var transactionTimeout = transactionOptions?.Timeout ?? clientSession.Options.DefaultTransactionOptions?.Timeout;
            using var operationContext = new OperationContext(clock, transactionTimeout, cancellationToken);

            while (true)
            {
                attempt++;
                clientSession.StartTransaction(transactionOptions);
                clientSession.WrappedCoreSession.CurrentTransaction.OperationContext = operationContext;

                try
                {
                    var result = await callbackAsync(clientSession, cancellationToken).ConfigureAwait(false);
                    // Transaction could be completed by user's code inside the callback, skipping commit in such case.
                    if (IsTransactionInStartingOrInProgressState(clientSession))
                    {
                        await CommitWithRetriesAsync(operationContext, clientSession, cancellationToken).ConfigureAwait(false);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    if(ShouldAbortTransaction(operationContext, clientSession, out var abortOptions))
                    {
                        await clientSession.AbortTransactionAsync(abortOptions, cancellationToken).ConfigureAwait(false);
                    }

                    if (!ShouldRetryTransaction(operationContext, ex, random, attempt, out var delay))
                    {
                        throw;
                    }

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static bool IsTimedOut(OperationContext operationContext, TimeSpan delay = default)
        {
            if (operationContext.Timeout.HasValue)
            {
                return operationContext.Elapsed + delay >= operationContext.Timeout;
            }

            return operationContext.RootContext.Elapsed + delay >= __transactionTimeout;
        }

        private static void CommitWithRetries(OperationContext operationContext, IClientSessionHandle clientSession, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    CommitTransactionOptions commitOptions = null;
                    if (operationContext.IsRootContextTimeoutConfigured())
                    {
                        commitOptions = new CommitTransactionOptions(operationContext.RemainingTimeout);
                    }

                    clientSession.CommitTransaction(commitOptions, cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    if (ShouldRetryCommit(operationContext, ex))
                    {
                        continue;
                    }

                    throw;
                }
            }
        }

        private static async Task CommitWithRetriesAsync(OperationContext operationContext, IClientSessionHandle clientSession, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    CommitTransactionOptions commitOptions = null;
                    if (operationContext.IsRootContextTimeoutConfigured())
                    {
                        commitOptions = new CommitTransactionOptions(operationContext.RemainingTimeout);
                    }

                    await clientSession.CommitTransactionAsync(commitOptions, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    if (ShouldRetryCommit(operationContext, ex))
                    {
                        continue;
                    }

                    throw;
                }
            }
        }

        private static bool HasErrorLabel(Exception ex, string errorLabel)
        {
            if (ex is MongoException mongoException)
            {
                return mongoException.HasErrorLabel(errorLabel);
            }

            return false;
        }

        private static bool IsMaxTimeMSExpiredException(Exception ex)
        {
            if (ex is MongoExecutionTimeoutException timeoutException &&
                timeoutException.Code == (int)ServerErrorCode.MaxTimeMSExpired)
            {
                return true;
            }

            if (ex is MongoWriteConcernException writeConcernException)
            {
                var writeConcernError = writeConcernException.WriteConcernResult.Response?.GetValue("writeConcernError", null)?.AsBsonDocument;
                if (writeConcernError != null)
                {
                    var code = writeConcernError.GetValue("code", -1).ToInt32();
                    if (code == (int)ServerErrorCode.MaxTimeMSExpired)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsTransactionInStartingOrInProgressState(IClientSession clientSession)
        {
            var currentTransaction = clientSession.WrappedCoreSession.CurrentTransaction;
            if (currentTransaction == null)
            {
                return false;
            }
            else
            {
                var transactionState = currentTransaction.State;
                return transactionState == CoreTransactionState.Starting || transactionState == CoreTransactionState.InProgress;
            }
        }


        private static bool ShouldAbortTransaction(OperationContext operationContext, IClientSession clientSession, out AbortTransactionOptions abortOptions)
        {
            abortOptions = null;
            if (IsTransactionInStartingOrInProgressState(clientSession))
            {
                if (operationContext.IsRootContextTimeoutConfigured())
                {
                    abortOptions = new AbortTransactionOptions(operationContext.RootContext.Timeout);
                }

                return true;
            }

            return false;
        }

        private static bool ShouldRetryCommit(OperationContext operationContext, Exception ex)
        {
            return
                HasErrorLabel(ex, UnknownTransactionCommitResultLabel) &&
                !IsTimedOut(operationContext) &&
                !IsMaxTimeMSExpiredException(ex);
        }

        private static bool ShouldRetryTransaction(OperationContext operationContext, Exception ex, IRandom random, int attempt, out TimeSpan delay)
        {
            if (!HasErrorLabel(ex, TransientTransactionErrorLabel))
            {
                delay = TimeSpan.Zero;
                return false;
            }

            delay = TimeSpan.FromMilliseconds(RetryabilityHelper.GetRetryDelayMs(random, attempt, 1.5, 5, 500));
            if (IsTimedOut(operationContext, delay))
            {
                delay = TimeSpan.Zero;
                return false;
            }

            return true;
        }
    }
}
