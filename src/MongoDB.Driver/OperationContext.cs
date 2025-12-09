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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal sealed class OperationContext : IDisposable
    {
        // TODO: this static field is temporary here and will be removed in a future PRs in scope of CSOT.
        public static readonly OperationContext NoTimeout = new(null, CancellationToken.None);

        private CancellationTokenSource _remainingTimeoutCancellationTokenSource;
        private CancellationTokenSource _combinedCancellationTokenSource;

        public OperationContext(TimeSpan? timeout, CancellationToken cancellationToken)
            : this(SystemClock.Instance, SystemClock.Instance.GetTimestamp(), timeout, cancellationToken)
        {
        }

        internal OperationContext(IClock clock, TimeSpan? timeout, CancellationToken cancellationToken)
            : this(clock, clock.GetTimestamp(), timeout, cancellationToken)
        {
        }

        internal OperationContext(IClock clock, long initialTimestamp, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            Clock = Ensure.IsNotNull(clock, nameof(clock));
            InitialTimestamp = initialTimestamp;
            Timeout = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(timeout, nameof(timeout));
            CancellationToken = cancellationToken;
            RootContext = this;
        }

        public CancellationToken CancellationToken { get; }

        public OperationContext RootContext { get; private init; }

        // OpenTelemetry operation metadata
        internal string OperationName { get; init; }
        internal string DatabaseName { get; init; }
        internal string CollectionName { get; init; }
        internal bool IsTracingEnabled { get; init; }

        public TimeSpan RemainingTimeout
        {
            get
            {
                if (Timeout == null || Timeout == System.Threading.Timeout.InfiniteTimeSpan)
                {
                    return System.Threading.Timeout.InfiniteTimeSpan;
                }

                var result = Timeout.Value - Elapsed;
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
        private long InitialTimestamp { get; }

        private IClock Clock { get; }

        public TimeSpan Elapsed
        {
            get
            {
                var totalSeconds = (Clock.GetTimestamp() - InitialTimestamp) / (double)Clock.Frequency;
                return TimeSpan.FromSeconds(totalSeconds);
            }
        }

        public TimeSpan? Timeout { get; }

        public void Dispose()
        {
            _remainingTimeoutCancellationTokenSource?.Dispose();
            _combinedCancellationTokenSource?.Dispose();
        }

        public OperationContext Fork() =>
            new (Clock, InitialTimestamp, Timeout, CancellationToken)
            {
                RootContext = RootContext
            };

        public bool IsTimedOut()
        {
            // Dotnet APIs like task.WaitAsync truncating the timeout to milliseconds.
            // We should truncate the remaining timeout to the milliseconds, in order to maintain the consistent state:
            // if operationContext.WaitTaskAsync() failed with TimeoutException, we want IsTimedOut() returns true.
            return (int)RemainingTimeout.TotalMilliseconds == 0;
        }

        public bool IsCancelledOrTimedOut() => IsTimedOut() || CancellationToken.IsCancellationRequested;

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
            task.WaitTask(RemainingTimeout, CancellationToken);
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

            return new OperationContext(Clock, timeout, CancellationToken)
            {
                RootContext = RootContext
            };
        }

        internal OperationContext WithOperationMetadata(string operationName, string databaseName, string collectionName, bool isTracingEnabled)
        {
            return new OperationContext(Clock, InitialTimestamp, Timeout, CancellationToken)
            {
                RootContext = RootContext,
                OperationName = operationName,
                DatabaseName = databaseName,
                CollectionName = collectionName,
                IsTracingEnabled = isTracingEnabled
            };
        }
    }
}
