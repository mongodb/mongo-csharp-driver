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
        private readonly object _syncRoot;

        public SemaphoreSlimSignalable(int count)
        {
            Ensure.IsBetween(count, 0, 1024, nameof(count));

            _semaphore = new SemaphoreSlim(count);
            _syncRoot = new object();

            _signalCancelationTokenSource = new CancellationTokenSource();
        }

        public int Count => _semaphore.CurrentCount;

        public void Signal()
        {
            if (!_signalCancelationTokenSource.IsCancellationRequested)
            {
                lock (_syncRoot)
                {
                    _signalCancelationTokenSource.Cancel();
                }
            }
        }

        public void Reset()
        {
            if (_signalCancelationTokenSource.IsCancellationRequested)
            {
                lock (_syncRoot)
                {
                    if (_signalCancelationTokenSource.IsCancellationRequested)
                    {
                        _signalCancelationTokenSource = new CancellationTokenSource();
                    }
                }
            }
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
            var (tokenSourceLinked, signalTokenSource, signaled) = GetLinkedTokenAndCheckForSignaled(cancellationToken);

            if (signaled)
            {
                return SemaphoreWaitResult.Signaled;
            }

            try
            {
                var entered = _semaphore.Wait(timeout, tokenSourceLinked.Token);
                return entered ? SemaphoreWaitResult.Entered : SemaphoreWaitResult.TimedOut;
            }
            catch (OperationCanceledException)
            {
                if (IsSignaled(signalTokenSource.Token, cancellationToken))
                {
                    return SemaphoreWaitResult.Signaled;
                }

                throw;
            }
        }

        public async Task<SemaphoreWaitResult> WaitSignaledAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var (tokemSourceLinked, signalTokenSource, signaled) = GetLinkedTokenAndCheckForSignaled(cancellationToken);

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
                if (IsSignaled(signalTokenSource.Token, cancellationToken))
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

        private (CancellationTokenSource TokenSourceLinked, CancellationTokenSource SignalTokenSource, bool Signaled) GetLinkedTokenAndCheckForSignaled(CancellationToken cancellationToken)
        {
            var signalTokenSource = _signalCancelationTokenSource;

            if (IsSignaled(signalTokenSource.Token, cancellationToken))
            {
                return (default, default, true);
            }

            var tokenSourceLinked = CancellationTokenSource.CreateLinkedTokenSource(
                signalTokenSource.Token,
                cancellationToken);

            return (tokenSourceLinked, signalTokenSource, false);
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
