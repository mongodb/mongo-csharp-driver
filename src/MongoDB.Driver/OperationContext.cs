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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
#if !NET6_0_OR_GREATER
using MongoDB.Driver.Core.Misc;
#endif

namespace MongoDB.Driver
{
    internal sealed class OperationContext
    {
        // TODO: this static field is temporary here and will be removed in a future PRs in scope of CSOT.
        public static readonly OperationContext NoTimeout = new(System.Threading.Timeout.InfiniteTimeSpan, CancellationToken.None);

        public OperationContext(TimeSpan timeout, CancellationToken cancellationToken)
            : this(Stopwatch.StartNew(), timeout, cancellationToken)
        {
        }

        internal OperationContext(Stopwatch stopwatch, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Stopwatch = stopwatch;
            Timeout = timeout;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public OperationContext ParentContext { get; private init; }

        public TimeSpan RemainingTimeout
        {
            get
            {
                if (Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                {
                    return System.Threading.Timeout.InfiniteTimeSpan;
                }

                return Timeout - Stopwatch.Elapsed;
            }
        }

        private Stopwatch Stopwatch { get; }

        private TimeSpan Timeout { get; }

        public bool IsTimedOut()
        {
            var remainingTimeout = RemainingTimeout;
            if (remainingTimeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                return false;
            }

            return remainingTimeout < TimeSpan.Zero;
        }

        public void WaitTask(Task task)
        {
            if (task.IsCompleted)
            {
                task.GetAwaiter().GetResult(); // re-throws exception if any
                return;
            }

            var timeout = RemainingTimeout;
            if (timeout != System.Threading.Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
            {
                throw new TimeoutException();
            }

            try
            {
                if (!task.Wait((int)timeout.TotalMilliseconds, CancellationToken))
                {
                    CancellationToken.ThrowIfCancellationRequested();
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

        public async Task WaitTaskAsync(Task task)
        {
            if (task.IsCompleted)
            {
                await task.ConfigureAwait(false); // re-throws exception if any
                return;
            }

            var timeout = RemainingTimeout;
            if (timeout != System.Threading.Timeout.InfiniteTimeSpan && timeout < TimeSpan.Zero)
            {
                throw new TimeoutException();
            }

            try
            {
                await task.WaitAsync(timeout, CancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        public OperationContext WithTimeout(TimeSpan timeout)
        {
            var remainingTimeout = RemainingTimeout;
            if (timeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                timeout = remainingTimeout;
            }
            else if (remainingTimeout != System.Threading.Timeout.InfiniteTimeSpan && remainingTimeout < timeout)
            {
                timeout = remainingTimeout;
            }

            return new OperationContext(timeout, CancellationToken)
            {
                ParentContext = this
            };
        }
    }
}

