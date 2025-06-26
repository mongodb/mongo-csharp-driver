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
    internal sealed class OperationContext : IDisposable
    {
        // TODO: this static field is temporary here and will be removed in a future PRs in scope of CSOT.
        public static readonly OperationContext NoTimeout = new(System.Threading.Timeout.InfiniteTimeSpan, CancellationToken.None);

        private CancellationTokenSource _remainingTimeoutCancellationTokenSource;
        private CancellationTokenSource _combinedCancellationTokenSource;

        public OperationContext(TimeSpan timeout, CancellationToken cancellationToken)
            : this(Stopwatch.StartNew(), timeout, cancellationToken)
        {
        }

        internal OperationContext(Stopwatch stopwatch, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Stopwatch = stopwatch;
            Timeout = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(timeout, nameof(timeout));
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

                var result = Timeout - Stopwatch.Elapsed;
                if (result < TimeSpan.Zero)
                {
                    result = TimeSpan.Zero;
                }

                return result;
            }
        }

        [Obsolete("Do not use this property, unless it's needed to avoid breaking changes in public API")]
        public CancellationToken CombinedCancellationToken
        {
            get
            {
                if (_combinedCancellationTokenSource != null)
                {
                    return _combinedCancellationTokenSource.Token;
                }

                if (RemainingTimeout == System.Threading.Timeout.InfiniteTimeSpan)
                {
                    return CancellationToken;
                }

                _remainingTimeoutCancellationTokenSource = new CancellationTokenSource(RemainingTimeout);
                _combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, _remainingTimeoutCancellationTokenSource.Token);
                return _combinedCancellationTokenSource.Token;
            }
        }
        private Stopwatch Stopwatch { get; }

        public TimeSpan Timeout { get; }

        public void Dispose()
        {
            _remainingTimeoutCancellationTokenSource?.Dispose();
            _combinedCancellationTokenSource?.Dispose();
        }

        public bool IsTimedOut()
            => RemainingTimeout == TimeSpan.Zero;

        public void ThrowIfTimedOutOrCanceled()
        {
            CancellationToken.ThrowIfCancellationRequested();
            if (IsTimedOut())
            {
                throw new TimeoutException();
            }
        }

        public void WaitTask(Task task)
        {
            if (task.IsCompleted)
            {
                task.GetAwaiter().GetResult(); // re-throws exception if any
                return;
            }

            var timeout = RemainingTimeout;
            if (timeout == TimeSpan.Zero)
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
            if (timeout == TimeSpan.Zero)
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
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(timeout, nameof(timeout));

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
                ParentContext = this,
            };
        }
    }
}

