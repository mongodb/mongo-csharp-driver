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

namespace MongoDB.Driver.Core.Operations
{
    internal static class RetryableReadOperationExecutor
    {
        // public static methods
        public static TResult Execute<TResult>(IRetryableReadOperation<TResult> operation, IReadBinding binding, bool retryRequested, CancellationToken cancellationToken)
        {
            using (var context = RetryableReadContext.Create(binding, retryRequested, cancellationToken))
            {
                return Execute(operation, context, cancellationToken);
            }
        }

        public static TResult Execute<TResult>(IRetryableReadOperation<TResult> operation, RetryableReadContext context, CancellationToken cancellationToken)
        {
            if (!ShouldReadBeRetried(context))
            {
                return operation.ExecuteAttempt(context, attempt: 1, transactionNumber: null, cancellationToken);
            }

            Exception originalException;
            try
            {
                return operation.ExecuteAttempt(context, attempt: 1, transactionNumber: null, cancellationToken);

            }
            catch (Exception ex) when (RetryabilityHelper.IsRetryableReadException(ex))
            {
                originalException = ex;
            }

            try
            {
                context.ReplaceChannelSource(context.Binding.GetReadChannelSource(new[] { context.ChannelSource.ServerDescription }, cancellationToken));
                context.ReplaceChannel(context.ChannelSource.GetChannel(cancellationToken));
            }
            catch
            {
                throw originalException;
            }

            try
            {
                return operation.ExecuteAttempt(context, attempt: 2, transactionNumber: null, cancellationToken);
            }
            catch (Exception ex) when (ShouldThrowOriginalException(ex))
            {
                throw originalException;
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(IRetryableReadOperation<TResult> operation, IReadBinding binding, bool retryRequested, CancellationToken cancellationToken)
        {
            using (var context = await RetryableReadContext.CreateAsync(binding, retryRequested, cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteAsync(operation, context, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(IRetryableReadOperation<TResult> operation, RetryableReadContext context, CancellationToken cancellationToken)
        {
            if (!ShouldReadBeRetried(context))
            {
                return await operation.ExecuteAttemptAsync(context, attempt: 1, transactionNumber: null, cancellationToken).ConfigureAwait(false);
            }

            Exception originalException;
            try
            {
                return await operation.ExecuteAttemptAsync(context, attempt: 1, transactionNumber: null, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (RetryabilityHelper.IsRetryableReadException(ex))
            {
                originalException = ex;
            }

            try
            {
                context.ReplaceChannelSource(context.Binding.GetReadChannelSource(new[] { context.ChannelSource.ServerDescription }, cancellationToken));
                context.ReplaceChannel(context.ChannelSource.GetChannel(cancellationToken));
            }
            catch
            {
                throw originalException;
            }

            try
            {
                return await operation.ExecuteAttemptAsync(context, attempt: 2, transactionNumber: null, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldThrowOriginalException(ex))
            {
                throw originalException;
            }
        }

        public static bool ShouldConnectionAcquireBeRetried(RetryableReadContext context, Exception ex)
        {
            // According the spec error during handshake should be handle according to RetryableReads logic
            var innerException = ex is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : ex;
            return context.RetryRequested && !context.Binding.Session.IsInTransaction && RetryabilityHelper.IsRetryableReadException(innerException);
        }

        // private static methods
        private static bool ShouldReadBeRetried(RetryableReadContext context)
        {
            return context.RetryRequested && !context.Binding.Session.IsInTransaction;
        }

        private static bool ShouldThrowOriginalException(Exception retryException)
        {
            return retryException is MongoException && !(retryException is MongoConnectionException);
        }
    }
}
