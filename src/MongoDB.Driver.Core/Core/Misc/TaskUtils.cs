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

using System.Threading.Tasks;
using System.Threading;
using System;

namespace MongoDB.Driver.Core.Misc
{
    internal static class TaskUtils
    {
        public static TResult RunCallbackOrThrow<TResult>(
            Task<TResult> task,
            TimeSpan timeout,
            string timeoutErrorMessage,
            CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(task, nameof(task));
            Ensure.IsNotNull(timeoutErrorMessage, nameof(timeoutErrorMessage));

            var resultIndex = Task.WaitAny(new[] { task }, (int)timeout.TotalMilliseconds, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (resultIndex == 0)
            {
                return task.GetAwaiter().GetResult();
            }
            else
            {
                throw new TimeoutException(timeoutErrorMessage);
            }
        }

        public static async Task<TResult> RunAsyncCallbackOrThrow<TResult>(
            Task<TResult> task,
            TimeSpan timeout,
            string timeoutErrorMessage,
            CancellationToken cancellationToken)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Ensure.IsNotNull(task, nameof(task));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Ensure.IsNotNull(timeoutErrorMessage, nameof(timeoutErrorMessage));

            var taskDelay = Task.Delay(timeout, cancellationToken);
            var result = await Task.WhenAny(task, taskDelay).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (result != taskDelay)
            {
                return await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException(timeoutErrorMessage);
            }
        }
    }
}
