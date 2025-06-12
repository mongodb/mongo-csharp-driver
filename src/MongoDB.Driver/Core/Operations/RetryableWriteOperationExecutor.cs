/* Copyright 2017-present MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Operations
{
    internal static class RetryableWriteOperationExecutor
    {
        // public static methods
        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, IWriteBinding binding, bool retryRequested)
        {
            using (var context = RetryableWriteContext.Create(operationContext, binding, retryRequested))
            {
                return Execute(operationContext, operation, context);
            }
        }

        public static TResult Execute<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            if (!AreRetriesAllowed(operation, context))
            {
                return operation.ExecuteAttempt(operationContext, context, 1, null);
            }

            var transactionNumber = context.Binding.Session.AdvanceTransactionNumber();
            Exception originalException;
            try
            {
                return operation.ExecuteAttempt(operationContext, context, 1, transactionNumber);
            }
            catch (Exception ex) when (RetryabilityHelper.IsRetryableWriteException(ex))
            {
                originalException = ex;
            }

            try
            {
                context.ReplaceChannelSource(context.Binding.GetWriteChannelSource(operationContext, new[] { context.ChannelSource.ServerDescription }));
                context.ReplaceChannel(context.ChannelSource.GetChannel(operationContext));
            }
            catch
            {
                throw originalException;
            }

            if (!AreRetryableWritesSupported(context.Channel.ConnectionDescription))
            {
                throw originalException;
            }

            try
            {
                return operation.ExecuteAttempt(operationContext, context, 2, transactionNumber);
            }
            catch (Exception ex) when (ShouldThrowOriginalException(ex))
            {
                throw originalException;
            }
        }

        public async static Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, IWriteBinding binding, bool retryRequested)
        {
            using (var context = await RetryableWriteContext.CreateAsync(operationContext, binding, retryRequested).ConfigureAwait(false))
            {
                return await ExecuteAsync(operationContext, operation, context).ConfigureAwait(false);
            }
        }

        public static async Task<TResult> ExecuteAsync<TResult>(OperationContext operationContext, IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            if (!AreRetriesAllowed(operation, context))
            {
                return await operation.ExecuteAttemptAsync(operationContext, context, 1, null).ConfigureAwait(false);
            }

            var transactionNumber = context.Binding.Session.AdvanceTransactionNumber();
            Exception originalException;
            try
            {
                return await operation.ExecuteAttemptAsync(operationContext, context, 1, transactionNumber).ConfigureAwait(false);
            }
            catch (Exception ex) when (RetryabilityHelper.IsRetryableWriteException(ex))
            {
                originalException = ex;
            }

            try
            {
                context.ReplaceChannelSource(await context.Binding.GetWriteChannelSourceAsync(operationContext, new[] { context.ChannelSource.ServerDescription }).ConfigureAwait(false));
                context.ReplaceChannel(await context.ChannelSource.GetChannelAsync(operationContext).ConfigureAwait(false));
            }
            catch
            {
                throw originalException;
            }

            if (!AreRetryableWritesSupported(context.Channel.ConnectionDescription))
            {
                throw originalException;
            }

            try
            {
                return await operation.ExecuteAttemptAsync(operationContext, context, 2, transactionNumber).ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldThrowOriginalException(ex))
            {
                throw originalException;
            }
        }

        public static bool ShouldConnectionAcquireBeRetried(RetryableWriteContext context, ServerDescription serverDescription, Exception exception)
        {
            var innerException = exception is MongoAuthenticationException mongoAuthenticationException ? mongoAuthenticationException.InnerException : exception;

            // According the spec error during handshake should be handle according to RetryableReads logic
            return context.RetryRequested &&
                AreRetryableWritesSupported(serverDescription) &&
                context.Binding.Session.Id != null &&
                !context.Binding.Session.IsInTransaction &&
                RetryabilityHelper.IsRetryableReadException(innerException);
        }

        // private static methods
        private static bool AreRetriesAllowed<TResult>(IRetryableWriteOperation<TResult> operation, RetryableWriteContext context)
        {
            return IsOperationAcknowledged(operation) && DoesContextAllowRetries(context);
        }

        private static bool AreRetryableWritesSupported(ConnectionDescription connectionDescription)
        {
            var helloResult = connectionDescription.HelloResult;
            return
                helloResult.ServerType == ServerType.LoadBalanced ||
                (helloResult.LogicalSessionTimeout != null && helloResult.ServerType != ServerType.Standalone);
        }

        private static bool AreRetryableWritesSupported(ServerDescription serverDescription)
        {
            return serverDescription.Type == ServerType.LoadBalanced ||
                (serverDescription.LogicalSessionTimeout != null && serverDescription.Type != ServerType.Standalone);
        }

        private static bool DoesContextAllowRetries(RetryableWriteContext context)
        {
            return
                context.RetryRequested &&
                AreRetryableWritesSupported(context.Channel.ConnectionDescription) &&
                context.Binding.Session.Id != null &&
                !context.Binding.Session.IsInTransaction;
        }

        private static bool IsOperationAcknowledged<TResult>(IRetryableWriteOperation<TResult> operation)
        {
            var writeConcern = operation.WriteConcern;
            return
                writeConcern == null || // null means use server default write concern which implies acknowledged
                writeConcern.IsAcknowledged;
        }

        private static bool ShouldThrowOriginalException(Exception retryException) =>
            retryException == null ||
            retryException is MongoException && !(retryException is MongoConnectionException || retryException is MongoConnectionPoolPausedException);
    }
}
