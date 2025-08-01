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
            CancellationToken cancellationToken)
        {
            var transactionTimeout = transactionOptions?.Timeout ?? clientSession.Options.DefaultTransactionOptions?.Timeout;
            using var operationContext = new OperationContext(clock, transactionTimeout, cancellationToken);

            while (true)
            {
                clientSession.StartTransaction(transactionOptions);
                clientSession.WrappedCoreSession.CurrentTransaction.OperationContext = operationContext;

                var callbackOutcome = ExecuteCallback(operationContext, clientSession, callback, cancellationToken);
                if (callbackOutcome.ShouldRetryTransaction)
                {
                    continue;
                }
                if (!IsTransactionInStartingOrInProgressState(clientSession))
                {
                    return callbackOutcome.Result; // assume callback intentionally ended the transaction
                }

                var transactionHasBeenCommitted = CommitWithRetries(operationContext, clientSession, cancellationToken);
                if (transactionHasBeenCommitted)
                {
                    return callbackOutcome.Result;
                }
            }
        }

        public static async Task<TResult> ExecuteWithRetriesAsync<TResult>(
            IClientSessionHandle clientSession,
            Func<IClientSessionHandle, CancellationToken, Task<TResult>> callbackAsync,
            TransactionOptions transactionOptions,
            IClock clock,
            CancellationToken cancellationToken)
        {
            TimeSpan? transactionTimeout = transactionOptions?.Timeout ?? clientSession.Options.DefaultTransactionOptions?.Timeout;
            using var operationContext = new OperationContext(clock, transactionTimeout, cancellationToken);

            while (true)
            {
                clientSession.StartTransaction(transactionOptions);
                clientSession.WrappedCoreSession.CurrentTransaction.OperationContext = operationContext;

                var callbackOutcome = await ExecuteCallbackAsync(operationContext, clientSession, callbackAsync, cancellationToken).ConfigureAwait(false);
                if (callbackOutcome.ShouldRetryTransaction)
                {
                    continue;
                }
                if (!IsTransactionInStartingOrInProgressState(clientSession))
                {
                    return callbackOutcome.Result; // assume callback intentionally ended the transaction
                }

                var transactionHasBeenCommitted = await CommitWithRetriesAsync(operationContext, clientSession, cancellationToken).ConfigureAwait(false);
                if (transactionHasBeenCommitted)
                {
                    return callbackOutcome.Result;
                }
            }
        }

        private static bool HasTimedOut(OperationContext operationContext)
        {
            return operationContext.IsTimedOut() ||
                   (operationContext.RootContext.Timeout == null && operationContext.RootContext.Elapsed > __transactionTimeout);
        }

        private static CallbackOutcome<TResult> ExecuteCallback<TResult>(OperationContext operationContext, IClientSessionHandle clientSession, Func<IClientSessionHandle, CancellationToken, TResult> callback, CancellationToken cancellationToken)
        {
            try
            {
                var result = callback(clientSession, cancellationToken);
                return new CallbackOutcome<TResult>.WithResult(result);
            }
            catch (Exception ex)
            {
                if (IsTransactionInStartingOrInProgressState(clientSession))
                {
                    AbortTransactionOptions abortOptions = null;
                    if (operationContext.IsRootContextTimeoutConfigured())
                    {
                        abortOptions = new AbortTransactionOptions(operationContext.RootContext.Timeout);
                    }

                    clientSession.AbortTransaction(abortOptions, cancellationToken);
                }

                if (HasErrorLabel(ex, TransientTransactionErrorLabel) && !HasTimedOut(operationContext))
                {
                    return new CallbackOutcome<TResult>.WithShouldRetryTransaction();
                }

                throw;
            }
        }

        private static async Task<CallbackOutcome<TResult>> ExecuteCallbackAsync<TResult>(OperationContext operationContext, IClientSessionHandle clientSession, Func<IClientSessionHandle, CancellationToken, Task<TResult>> callbackAsync, CancellationToken cancellationToken)
        {
            try
            {
                var result = await callbackAsync(clientSession, cancellationToken).ConfigureAwait(false);
                return new CallbackOutcome<TResult>.WithResult(result);
            }
            catch (Exception ex)
            {
                if (IsTransactionInStartingOrInProgressState(clientSession))
                {
                    AbortTransactionOptions abortOptions = null;
                    if (operationContext.IsRootContextTimeoutConfigured())
                    {
                        abortOptions = new AbortTransactionOptions(operationContext.RootContext.Timeout);
                    }

                    await clientSession.AbortTransactionAsync(abortOptions, cancellationToken).ConfigureAwait(false);
                }

                if (HasErrorLabel(ex, TransientTransactionErrorLabel) && !HasTimedOut(operationContext))
                {
                    return new CallbackOutcome<TResult>.WithShouldRetryTransaction();
                }

                throw;
            }
        }

        private static bool CommitWithRetries(OperationContext operationContext, IClientSessionHandle clientSession, CancellationToken cancellationToken)
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
                    return true;
                }
                catch (Exception ex)
                {
                    if (ShouldRetryCommit(operationContext, ex))
                    {
                        continue;
                    }

                    if (HasErrorLabel(ex, TransientTransactionErrorLabel) && !HasTimedOut(operationContext))
                    {
                        return false; // the transaction will be retried
                    }

                    throw;
                }
            }
        }

        private static async Task<bool> CommitWithRetriesAsync(OperationContext operationContext, IClientSessionHandle clientSession, CancellationToken cancellationToken)
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
                    return true;
                }
                catch (Exception ex)
                {
                    if (ShouldRetryCommit(operationContext, ex))
                    {
                        continue;
                    }

                    if (HasErrorLabel(ex, TransientTransactionErrorLabel) && !HasTimedOut(operationContext))
                    {
                        return false; // the transaction will be retried
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
            else
            {
                return false;
            }
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

        private static bool IsTransactionInStartingOrInProgressState(IClientSessionHandle clientSession)
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

        private static bool ShouldRetryCommit(OperationContext operationContext, Exception ex)
        {
            return
                HasErrorLabel(ex, UnknownTransactionCommitResultLabel) &&
                !HasTimedOut(operationContext) &&
                !IsMaxTimeMSExpiredException(ex);
        }

        // nested types
        internal abstract class CallbackOutcome<TResult>
        {
            public virtual TResult Result => throw new InvalidOperationException();
            public virtual bool ShouldRetryTransaction => false;

            public class WithResult : CallbackOutcome<TResult>
            {
                public WithResult(TResult result)
                {
                    Result = result;
                }

                public override TResult Result { get; }
            }

            public class WithShouldRetryTransaction : CallbackOutcome<TResult>
            {
                public override bool ShouldRetryTransaction => true;
            }
        }
    }
}
