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

        private readonly struct CancellationContext : IDisposable
        {
            public bool IsSignaled { get; }
            public CancellationToken CancellationToken { get; }
            public CancellationTokenSource LinkedCancellationTokenSource { get; }
            public CancellationTokenSource SignaledCancellationTokenSource { get; }

            public static CancellationContext Signaled { get; } = new CancellationContext(true, default, default, default);

            public CancellationContext(
                bool isSignaled,
                CancellationTokenSource linkedCancellationTokenSource,
                CancellationTokenSource signaledCancellationTokenSource,
                CancellationToken cancellationToken)
            {
                IsSignaled = isSignaled;
                CancellationToken = cancellationToken;
                LinkedCancellationTokenSource = linkedCancellationTokenSource;
                SignaledCancellationTokenSource = signaledCancellationTokenSource;
            }

            public void Dispose() => LinkedCancellationTokenSource?.Dispose();
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

        private readonly SemaphoreSlim _semaphore;
        private readonly object _syncRoot;

        public SemaphoreSlimSignalable(int initialCount)
        {
            // reasonable upper bound for initialCount to ensure overall correctness
            Ensure.IsBetween(initialCount, 0, 1024, nameof(initialCount));

            _semaphore = new SemaphoreSlim(initialCount);
            _syncRoot = new object();

            _signalCancellationTokenSource = new CancellationTokenSource();
        }

        public int Count => _semaphore.CurrentCount;

        public void Signal()
        {
            lock (_syncRoot)
            {
                _signalCancellationTokenSource.Cancel();
            }
        }

        public void Reset()
        {
            if (_signalCancellationTokenSource.IsCancellationRequested)
            {
                lock (_syncRoot)
                {
                    if (_signalCancellationTokenSource.IsCancellationRequested)
                    {
                        _signalCancellationTokenSource.Dispose();
                        _signalCancellationTokenSource = new CancellationTokenSource();
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
            using var cancellationContext = GetCancelationTokenContext(cancellationToken);

            if (cancellationContext.IsSignaled)
            {
                return SemaphoreWaitResult.Signaled;
            }

            try
            {

                var entered = _semaphore.Wait(timeout, cancellationContext.CancellationToken);
                return entered ? SemaphoreWaitResult.Entered : SemaphoreWaitResult.TimedOut;
            }
            catch (OperationCanceledException)
            {
                if (IsSignaled(cancellationContext.SignaledCancellationTokenSource, cancellationToken))
                {
                    return SemaphoreWaitResult.Signaled;
                }

                throw;
            }
        }

        public async Task<SemaphoreWaitResult> WaitSignaledAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var cancellationContext = GetCancelationTokenContext(cancellationToken);

            if (cancellationContext.IsSignaled)
            {
                return SemaphoreWaitResult.Signaled;
            }

            try
            {
                var entered = await _semaphore.WaitAsync(timeout, cancellationContext.CancellationToken).ConfigureAwait(false);

                return entered ? SemaphoreWaitResult.Entered : SemaphoreWaitResult.TimedOut;
            }
            catch (OperationCanceledException)
            {
                if (IsSignaled(cancellationContext.SignaledCancellationTokenSource, cancellationToken))
                {
                    // Request task rescheduling, to avoid resuming execution on Signal thread
                    await TaskExtensions.YieldNoContext();

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
            _signalCancellationTokenSource.Dispose();
        }

        private CancellationContext GetCancelationTokenContext(CancellationToken cancellationToken)
        {
            var signalTokenSource = _signalCancellationTokenSource;

            if (IsSignaled(signalTokenSource, cancellationToken))
            {
                return CancellationContext.Signaled;
            }

            CancellationTokenSource cancellationLinkedTokenSource = null;
            CancellationToken cancellationTokenResult;

            try
            {
                if (cancellationToken == default)
                {
                    cancellationTokenResult = signalTokenSource.Token;
                }
                else
                {
                    cancellationLinkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        signalTokenSource.Token,
                        cancellationToken);

                    cancellationTokenResult = cancellationLinkedTokenSource.Token;
                }
            }
            catch (ObjectDisposedException)
            {
                // signalTokenSource was disposed, it will happen only when cancellation was requested for signalTokenSource or on Dispose
                return CancellationContext.Signaled;
            }

            return new CancellationContext(false, cancellationLinkedTokenSource, signalTokenSource, cancellationTokenResult);
        }

        private bool IsSignaled(CancellationTokenSource signalTokenSource, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return signalTokenSource.Token.IsCancellationRequested;
            }
            catch (ObjectDisposedException)
            {
                // signalTokenSource was disposed, it will happen only when cancellation was requested for signalTokenSource or on Dispose
                return true;
            }
        }
    }
}
