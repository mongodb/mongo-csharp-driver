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

        public sealed class SemaphoreSlimSignalableAwaiter : IDisposable
        {
            private readonly SemaphoreSlimSignalable _semaphoreSlimSignalable;
            private bool _enteredSemaphore;

            public SemaphoreSlimSignalableAwaiter(SemaphoreSlimSignalable semaphoreSlimSignalable)
            {
                _semaphoreSlimSignalable = semaphoreSlimSignalable;
                _enteredSemaphore = false;
            }

            public async Task<bool> WaitSignaledAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                var waitResult = await _semaphoreSlimSignalable.WaitSignaledAsync(timeout, cancellationToken).ConfigureAwait(false);
                _enteredSemaphore = waitResult == SemaphoreWaitResult.Entered;
                return _enteredSemaphore;
            }

            public void Dispose()
            {
                if (_enteredSemaphore)
                {
                    _semaphoreSlimSignalable.Release();
                }
            }
        }

        // private fields
        private CancellationTokenSource _signalCancellationTokenSource;
        private bool _isCancellationScheduled;

        private readonly SemaphoreSlim _semaphore;
        private readonly object _syncRoot;

        public SemaphoreSlimSignalable(int initialCount)
        {
            // reasonable upper bound for initialCount to ensure overall correctness
            Ensure.IsBetween(initialCount, 0, 1024, nameof(initialCount));

            _semaphore = new SemaphoreSlim(initialCount);
            _syncRoot = new object();

            _signalCancellationTokenSource = new CancellationTokenSource();
            _isCancellationScheduled = false;
        }

        public int Count => _semaphore.CurrentCount;

        public void Signal()
        {
            if (!_isCancellationScheduled)
            {
                lock (_syncRoot)
                {
                    if (!_isCancellationScheduled)
                    {
                        _signalCancellationTokenSource.CancelAfter(0);
                        _isCancellationScheduled = true;
                    }
                }
            }
        }

        public void Reset()
        {
            if (_isCancellationScheduled)
            {
                lock (_syncRoot)
                {
                    if (_isCancellationScheduled)
                    {
                        _signalCancellationTokenSource = new CancellationTokenSource();
                        _isCancellationScheduled = false;
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

        public SemaphoreSlimSignalableAwaiter CreateAwaiter() =>
            new SemaphoreSlimSignalableAwaiter(this);

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
            var signalTokenSource = _signalCancellationTokenSource;

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
