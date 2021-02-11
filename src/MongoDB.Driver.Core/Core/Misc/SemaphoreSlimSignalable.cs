/* Copyright 2021-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Misc
{
    internal sealed class SemaphoreSlimSignalable : IDisposable
    {
        public enum SemaphoreWaitResult
        {
            None,
            Signaled,
            TimedOut,
            Entered
        }

        // private fields
        private CancellationTokenSource _signalCancelationTokenSource;

        private readonly SemaphoreSlim _semaphore;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreSlimRequest" /> class.
        /// </summary>
        /// <param name="count">The count.</param>
        public SemaphoreSlimSignalable(int count)
        {
            Ensure.IsBetween(count, 1, 1024, nameof(count));

            _semaphore = new SemaphoreSlim(count);

            _signalCancelationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Signals
        /// </summary>
        public void Signal()
        {
            _signalCancelationTokenSource?.Cancel();
        }

        /// <summary>
        /// Clears the signal
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _signalCancelationTokenSource, new CancellationTokenSource());
        }

        public SemaphoreWaitResult Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var entered = _semaphore.Wait(timeout, cancellationToken);
            return entered ? SemaphoreWaitResult.Entered : SemaphoreWaitResult.TimedOut;
        }

        public async Task<SemaphoreWaitResult> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var entered = await _semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);

            return entered ? SemaphoreWaitResult.Entered : SemaphoreWaitResult.TimedOut;
        }

        public SemaphoreWaitResult WaitSignaled(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var (tokemSourceLinked, signalToken, signaled) = GetLinkedTokenAndCheckForSignaled(cancellationToken);

            if (signaled)
            {
                return SemaphoreWaitResult.Signaled;
            }

            try
            {
                var entered = _semaphore.Wait(timeout, tokemSourceLinked.Token);
                return entered ? SemaphoreWaitResult.Entered : SemaphoreWaitResult.TimedOut;
            }
            catch (OperationCanceledException)
            {
                if (IsSignaled(signalToken, cancellationToken))
                {
                    return SemaphoreWaitResult.Signaled;
                }

                throw;
            }
        }

        public async Task<SemaphoreWaitResult> WaitSignaledAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var (tokemSourceLinked, signalToken, signaled) = GetLinkedTokenAndCheckForSignaled(cancellationToken);

            if (signaled)
            {
                return SemaphoreWaitResult.Signaled;
            }

            try
            {
                var entered = await _semaphore.WaitAsync(timeout, tokemSourceLinked.Token).ConfigureAwait(false);
                return entered ? SemaphoreWaitResult.Entered : SemaphoreWaitResult.TimedOut;
            }
            catch (OperationCanceledException)
            {
                if (IsSignaled(signalToken, cancellationToken))
                {
                    return SemaphoreWaitResult.Signaled;
                }

                throw;
            }
        }

        public void Release()
        {
            _semaphore.Release();
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        private (CancellationTokenSource TokenSourceLinked, CancellationToken SignalToken, bool Signaled) GetLinkedTokenAndCheckForSignaled(CancellationToken cancellationToken)
        {
            var signalToken = _signalCancelationTokenSource.Token;

            if (IsSignaled(signalToken, cancellationToken))
            {
                return (default, default, true);
            }

            var tokenSourceLinked = CancellationTokenSource.CreateLinkedTokenSource(
                signalToken,
                cancellationToken);

            return (tokenSourceLinked, signalToken, false);
        }

#pragma warning disable CA1068 // CancellationToken parameters must come last
        private bool IsSignaled(CancellationToken signalToken, CancellationToken cancellationToken)
#pragma warning restore CA1068 // CancellationToken parameters must come last
        {
            cancellationToken.ThrowIfCancellationRequested();

            return signalToken.IsCancellationRequested;
        }
    }
}
