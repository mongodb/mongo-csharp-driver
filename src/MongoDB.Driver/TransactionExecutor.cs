/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Driver.Support;

namespace MongoDB.Driver
{
    internal static class TransactionExecutor
    {
        // constants
        private const string TransientTransactionErrorLabel = "TransientTransactionError";
        private const string UnknownTransactionCommitResultLabel = "UnknownTransactionCommitResult";
        private const int ExceededTimeLimitErrorCode = 50;
        private static readonly TimeSpan __transactionTimeout = TimeSpan.FromSeconds(120);

        public static TResult ExecuteWithRetries<TResult>(
            IClientSessionHandle clientSession,
            Func<IClientSessionHandle, CancellationToken, TResult> callback,
            TransactionOptions transactionOptions,
            IClock clock,
            CancellationToken cancellationToken)
        {
            var startTime = clock.UtcNow;

            while (true)
            {
                clientSession.StartTransaction(transactionOptions);

                var callbackOutcome = ExecuteCallback(clientSession, callback, startTime, clock, cancellationToken);
                if (callbackOutcome.ShouldRetryTransaction)
                {
                    continue;
                }
                if (!IsTransactionInStartingOrInProgressState(clientSession))
                {
                    return callbackOutcome.Result; // assume callback intentionally ended the transaction
                }

                var transactionHasBeenCommitted = CommitWithRetries(clientSession, startTime, clock, cancellationToken);
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
            var startTime = clock.UtcNow;
            while (true)
            {
                clientSession.StartTransaction(transactionOptions);

                var callbackOutcome = await ExecuteCallbackAsync(clientSession, callbackAsync, startTime, clock, cancellationToken).ConfigureAwait(false);
                if (callbackOutcome.ShouldRetryTransaction)
                {
                    continue;
                }
                if (!IsTransactionInStartingOrInProgressState(clientSession))
                {
                    return callbackOutcome.Result; // assume callback intentionally ended the transaction
                }

                var transactionHasBeenCommitted = await CommitWithRetriesAsync(clientSession, startTime, clock, cancellationToken).ConfigureAwait(false);
                if (transactionHasBeenCommitted)
                {
                    return callbackOutcome.Result;
                }
            }
        }

        private static bool HasTimedOut(DateTime startTime, DateTime currentTime)
        {
            return (currentTime - startTime) >= __transactionTimeout;
        }

        private static CallbackOutcome<TResult> ExecuteCallback<TResult>(IClientSessionHandle clientSession, Func<IClientSessionHandle, CancellationToken, TResult> callback, DateTime startTime, IClock clock, CancellationToken cancellationToken)
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
                    clientSession.AbortTransaction(cancellationToken);
                }

                if (HasErrorLabel(ex, TransientTransactionErrorLabel) && !HasTimedOut(startTime, clock.UtcNow))
                {
                    return new CallbackOutcome<TResult>.WithShouldRetryTransaction();
                }

                throw;
            }
        }

        private static async Task<CallbackOutcome<TResult>> ExecuteCallbackAsync<TResult>(IClientSessionHandle clientSession, Func<IClientSessionHandle, CancellationToken, Task<TResult>> callbackAsync, DateTime startTime, IClock clock, CancellationToken cancellationToken)
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
                    await clientSession.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                }

                if (HasErrorLabel(ex, TransientTransactionErrorLabel) && !HasTimedOut(startTime, clock.UtcNow))
                {
                    return new CallbackOutcome<TResult>.WithShouldRetryTransaction();
                }

                throw;
            }
        }

        private static bool CommitWithRetries(IClientSessionHandle clientSession, DateTime startTime, IClock clock, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    clientSession.CommitTransaction(cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    var now = clock.UtcNow; // call UtcNow once since we need to facilitate predictable mocking
                    if (ShouldRetryCommit(ex, startTime, now))
                    {
                        continue;
                    }

                    if (HasErrorLabel(ex, TransientTransactionErrorLabel) && !HasTimedOut(startTime, now))
                    {
                        return false; // the transaction will be retried
                    }

                    throw;
                }
            }
        }

        private static async Task<bool> CommitWithRetriesAsync(IClientSessionHandle clientSession, DateTime startTime, IClock clock, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    await clientSession.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    var now = clock.UtcNow; // call UtcNow once since we need to facilitate predictable mocking
                    if (ShouldRetryCommit(ex, startTime, now))
                    {
                        continue;
                    }

                    if (HasErrorLabel(ex, TransientTransactionErrorLabel) && !HasTimedOut(startTime, now))
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

        private static bool IsExceededTimeLimitException(Exception ex)
        {
            if (ex is MongoExecutionTimeoutException timeoutException && 
                timeoutException.Code == ExceededTimeLimitErrorCode)
            {
                return true;
            }

            if (ex is MongoWriteConcernException writeConcernException)
            {
                var writeConcernError = writeConcernException.WriteConcernResult.Response?.GetValue("writeConcernError", null)?.AsBsonDocument;
                if (writeConcernError != null)
                {
                    var code = writeConcernError.GetValue("code", -1).ToInt32();
                    if (code == ExceededTimeLimitErrorCode)
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

        private static bool ShouldRetryCommit(Exception ex, DateTime startTime, DateTime now)
        {
            return
                HasErrorLabel(ex, UnknownTransactionCommitResultLabel) &&
                !HasTimedOut(startTime, now) &&
                !IsExceededTimeLimitException(ex);
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