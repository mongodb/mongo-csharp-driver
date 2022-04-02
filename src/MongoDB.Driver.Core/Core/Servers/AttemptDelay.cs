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
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Servers
{
    internal sealed class AttemptDelay : IDisposable
    {
        // fields
        private readonly DateTime _earlyAttemptAt;
        private int _earlyAttemptHasBeenRequested;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private readonly Timer _timer;

        // constructors
        public AttemptDelay(TimeSpan interval, TimeSpan minInterval)
        {
            if (interval != Timeout.InfiniteTimeSpan && minInterval > interval)
            {
                minInterval = interval;
            }
            _timer = new Timer(TimerCallback, null, interval, Timeout.InfiniteTimeSpan);
            _earlyAttemptAt = DateTime.UtcNow + minInterval;
        }

        // properties
        public bool EarlyAttemptHasBeenRequested
        {
            get { return _earlyAttemptHasBeenRequested == 1; }
        }

        public Task Task
        {
            get { return _taskCompletionSource.Task; }
        }

        // methods
        public void Dispose()
        {
            _timer.Dispose();
        }

        public void RequestNextAttempt()
        {
            if (Interlocked.CompareExchange(ref _earlyAttemptHasBeenRequested, 1, 0) == 0)
            {
                var earlyAttemptDelay = _earlyAttemptAt - DateTime.UtcNow;
                if (earlyAttemptDelay <= TimeSpan.Zero)
                {
                    _timer.Dispose();
                    _taskCompletionSource.TrySetResult(true);
                }
                else
                {
                    try
                    {
                        _timer.Change(earlyAttemptDelay, Timeout.InfiniteTimeSpan);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Allow timer to be disposed during RequestNextAttempt
                    }
                }
            }
        }

        public void Wait(CancellationToken cancellationToken)
        {
            _taskCompletionSource.Task.Wait(cancellationToken);
        }

        private void TimerCallback(object state)
        {
            _taskCompletionSource.TrySetResult(true);
        }
    }
}
