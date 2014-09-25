/* Copyright 2013-2014 MongoDB Inc.
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

namespace MongoDB.Driver.Core.Async
{
    public static class TaskExtensionMethods
    {
        // static methods
        public static void HandleUnobservedException(this Task task, Action<Exception> onError)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    onError(t.Exception);
                }
            });
        }

        public static TaskCompletionSource<T> WithCancellationToken<T>(this TaskCompletionSource<T> source, CancellationToken cancellationToken)
        {
            if (cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(() =>
                {
                    source.TrySetCanceled();
                });
            }
            return source;
        }

        public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            var source = new CancellationTokenSource();
            var delay = Task.Delay(timeout, source.Token);
            await Task.WhenAny(task, delay).ConfigureAwait(false);
            if (task.IsCompleted)
            {
                source.Cancel();
            }
            else
            {
                throw new TimeoutException();
            }
        }

        public static async Task WithTimeout(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var delayCancellationTokenSource = new CancellationTokenSource();
            var delayTask = Task.Delay(timeout, delayCancellationTokenSource.Token);
            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>();
            var registration = cancellationToken.Register(
                state =>
                {
                    var source = (TaskCompletionSource<bool>)state;
                    source.TrySetResult(true);
                },
                cancellationTaskCompletionSource);

            using (registration)
            {
                await Task.WhenAny(task, delayTask, cancellationTaskCompletionSource.Task).ConfigureAwait(false);
                if (task.IsCompleted)
                {
                    delayCancellationTokenSource.Cancel();
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                else
                {
                    throw new TimeoutException();
                }
            }
        }

        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            await ((Task)task).WithTimeout(timeout).ConfigureAwait(false);
            return await task.ConfigureAwait(false);
        }

        public static TaskCompletionSource<T> WithTimeout<T>(this TaskCompletionSource<T> source, TimeSpan timeout)
        {
            if (timeout != TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            {
                Task.Delay(timeout).ContinueWith(completedTask =>
                {
                    source.TrySetException(new TimeoutException());
                });
            }
            return source;
        }
    }
}
