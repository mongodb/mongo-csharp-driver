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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal sealed class OperationContext
    {
        // TODO: this static field is temporary here and will be removed in a future PRs in scope of CSOT.
        public static readonly OperationContext NoTimeout = new(System.Threading.Timeout.InfiniteTimeSpan, CancellationToken.None);

        private readonly Stopwatch _stopwatch;

        public OperationContext(TimeSpan timeout, CancellationToken cancellationToken)
            : this(Stopwatch.StartNew(), timeout, cancellationToken)
        {
        }

        internal OperationContext(Stopwatch stopwatch, TimeSpan timeout, CancellationToken cancellationToken)
        {
            _stopwatch = stopwatch;
            Timeout = timeout;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public TimeSpan Timeout { get; }

        public TimeSpan RemainingTimeout
        {
            get
            {
                if (Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                {
                    return System.Threading.Timeout.InfiniteTimeSpan;
                }

                return Timeout - _stopwatch.Elapsed;
            }
        }

        public bool IsTimedOut()
        {
            var remainingTimeout = RemainingTimeout;
            if (remainingTimeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                return false;
            }

            return remainingTimeout < TimeSpan.Zero;
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

            return new OperationContext(timeout, CancellationToken);
        }

        public void WaitTask(Task task)
        {
            var timeout = RemainingTimeout;
            if (timeout < TimeSpan.Zero)
            {
                throw new TimeoutException();
            }

            if (!task.Wait((int)timeout.TotalMilliseconds, CancellationToken))
            {
                throw new TimeoutException();
            }
        }

        public async Task WaitTaskAsync(Task task)
        {
            var timeout = RemainingTimeout;
            if (timeout < TimeSpan.Zero)
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
    }
}

