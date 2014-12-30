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
