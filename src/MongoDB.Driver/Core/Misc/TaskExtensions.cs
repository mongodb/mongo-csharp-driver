/* Copyright 2013-present MongoDB Inc.
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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Misc
{
    internal static class TaskExtensions
    {
        public static void WaitTask(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(timeout, nameof(timeout));
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (task.IsCompleted)
                {
                    task.GetAwaiter().GetResult(); // re-throws exception if any
                    return;
                }

                if (task.Wait((int)timeout.TotalMilliseconds, cancellationToken))
                {
                    task.GetAwaiter().GetResult(); // re-throws exception if any
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException();
                }
            }
            catch (AggregateException e)
            {
                if (e.InnerExceptions.Count == 1)
                {
                    throw e.InnerExceptions[0];
                }

                throw;
            }
        }

#if !NET6_0_OR_GREATER
        public static Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            EnsureTimeoutIsValid(timeout);
            return WaitAsyncCore(task, timeout, cancellationToken);

            static async Task WaitAsyncCore(Task task, TimeSpan timeout, CancellationToken cancellationToken)
            {
                if (!task.IsCompleted)
                {
                    var timeoutTask = Task.Delay(timeout, cancellationToken);
                    await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);
                }

                if (task.IsCompleted)
                {
                    // will re-throw the exception if any
                    await task.ConfigureAwait(false);
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
                throw new TimeoutException();
            }
        }

        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            EnsureTimeoutIsValid(timeout);
            return WaitAsyncCore(task, timeout, cancellationToken);

            static async Task<TResult> WaitAsyncCore(Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken)
            {
                if (!task.IsCompleted)
                {
                    var timeoutTask = Task.Delay(timeout, cancellationToken);
                    await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);
                }

                if (task.IsCompleted)
                {
                    // will return the result or re-throw the exception if any
                    return await task.ConfigureAwait(false);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }

                throw new TimeoutException();
            }
        }

        private static void EnsureTimeoutIsValid(TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                return;
            }

            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
        }
#endif

        internal struct YieldNoContextAwaitable
        {
            public YieldNoContextAwaiter GetAwaiter() { return new YieldNoContextAwaiter(); }

            public struct YieldNoContextAwaiter : ICriticalNotifyCompletion
            {
                /// <summary>Gets whether a yield is not required.</summary>
                /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
                public bool IsCompleted { get { return false; } } // yielding is always required for YieldNoContextAwaiter, hence false

                public void OnCompleted(Action continuation)
                {
                    Task.Factory.StartNew(continuation, default, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
                }

                public void UnsafeOnCompleted(Action continuation)
                {
                    Task.Factory.StartNew(continuation, default, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
                }

                public void GetResult()
                {
                    // no op
                }
            }
        }

        public static void IgnoreExceptions(this Task task)
        {
            task.ContinueWith(t => { var ignored = t.Exception; },
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously);
        }

        public static YieldNoContextAwaitable YieldNoContext() => new YieldNoContextAwaitable();
    }
}
