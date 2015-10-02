/* Copyright 2015 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Async
{
    internal sealed class AsyncLock : IDisposable
    {
        // private static fields
        private static readonly Task __completedTask = Task.FromResult(true);

        // private fields
        private bool _disposed;
        private bool _taken;
        private readonly AsyncLockRequest _uncontestedRequest;
        private readonly Queue<TaskCompletionSource<bool>> _waiters;

        // constructors
        public AsyncLock()
        {
            _uncontestedRequest = new AsyncLockRequest(this, null, __completedTask, CancellationToken.None);
            _waiters = new Queue<TaskCompletionSource<bool>>();
        }

        // public methods
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")] // TODO: why is this needed?
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _uncontestedRequest.Dispose();
            }
        }

        public AsyncLockRequest Request(CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            lock (_waiters)
            {
                if (!_taken)
                {
                    _taken = true;
                    return _uncontestedRequest;
                }
                else
                {
                    var waiter = new TaskCompletionSource<bool>();
                    _waiters.Enqueue(waiter);
                    return new AsyncLockRequest(this, waiter, waiter.Task, cancellationToken);
                }
            }
        }

        // private methods
        private void Cancel(TaskCompletionSource<bool> waiter)
        {
            lock (_waiters)
            {
                RemoveWaiter(waiter);
            }

            waiter.TrySetCanceled();
        }

        private void Release(TaskCompletionSource<bool> waiter)
        {
            TaskCompletionSource<bool> cancel = null;
            TaskCompletionSource<bool> next = null;

            lock (_waiters)
            {
                if (waiter != null && waiter.Task.Status != TaskStatus.RanToCompletion)
                {
                    RemoveWaiter(waiter);
                    cancel = waiter;
                }
                else if (_waiters.Count == 0)
                {
                    _taken = false;
                    return;
                }
                else
                {
                    next = _waiters.Dequeue();
                }
            }

            cancel?.TrySetCanceled();
            next?.TrySetResult(true);
        }

        private void RemoveWaiter(TaskCompletionSource<bool> waiter)
        {
            // TODO: write a Queue implementation that supports removing items from the middle of the queue?
            var otherWaiters = _waiters.Where(w => w != waiter).ToList();
            if (otherWaiters.Count < _waiters.Count)
            {
                _waiters.Clear();
                foreach (var otherWaiter in otherWaiters)
                {
                    _waiters.Enqueue(otherWaiter);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // nested types
        public struct AsyncLockRequest : IDisposable
        {
            private readonly AsyncLock _lock;
            private readonly CancellationTokenRegistration _registration;
            private readonly Task _task;
            private readonly TaskCompletionSource<bool> _waiter;

            public AsyncLockRequest(AsyncLock @lock, TaskCompletionSource<bool> waiter, Task task, CancellationToken cancellationToken)
            {
                _lock = @lock;
                _waiter = waiter;
                _task = task;
                _registration = waiter != null  ? cancellationToken.Register(() => @lock.Cancel(waiter)) : default(CancellationTokenRegistration);
            }

            public Task Task
            {
                get { return _task; }
            }

            public void Dispose()
            {
                _registration.Dispose();
                _lock.Release(_waiter);
            }
        }
    }
}
