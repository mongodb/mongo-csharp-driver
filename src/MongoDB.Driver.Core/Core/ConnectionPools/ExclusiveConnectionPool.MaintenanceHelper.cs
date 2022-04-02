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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal sealed partial class ExclusiveConnectionPool
    {
        internal sealed class MaintenanceHelper : IDisposable
        {
            private readonly ExclusiveConnectionPool _connectionPool;
            private Thread _maintenanceThread;
            private MaintenanceExecutingManager _maintenanceExecutingManager;
            private readonly CancellationTokenSource _maintenanceHelperCancellationTokenSource;
            private readonly CancellationToken _maintenanceHelperCancellationToken;

            public MaintenanceHelper(ExclusiveConnectionPool connectionPool, TimeSpan interval)
            {
                _connectionPool = Ensure.IsNotNull(connectionPool, nameof(connectionPool));
                _maintenanceExecutingManager = new MaintenanceExecutingManager(interval);

                _maintenanceHelperCancellationTokenSource = new CancellationTokenSource();
                _maintenanceHelperCancellationToken = _maintenanceHelperCancellationTokenSource.Token;
            }

            public bool IsRunning => _maintenanceThread != null;

            public void RequestStoppingMaintenance(bool closeInProgressConnection)
            {
                _maintenanceThread = null;

                _maintenanceExecutingManager.RequestNextAttempt(closeInProgressConnection);
                // the previous _maintenanceThread might not be stopped yet, but it will be soon
            }

            public void Start()
            {
                _maintenanceThread = new Thread(ThreadStart) { IsBackground = true };
                _maintenanceThread.Start();

                void ThreadStart()
                {
                    MaintainSize();
                }
            }

            public void Dispose()
            {
                _maintenanceExecutingManager.Dispose();
                _maintenanceHelperCancellationTokenSource.Cancel();
                _maintenanceHelperCancellationTokenSource.Dispose();
            }

            // private methods
            private void MaintainSize()
            {
                _maintenanceExecutingManager.Refresh();

                try
                {
                    while (!_maintenanceHelperCancellationTokenSource.IsCancellationRequested && !_maintenanceExecutingManager.StopLoop)
                    {
                        try
                        {
                            _connectionPool._connectionHolder.Prune(_maintenanceExecutingManager.CloseInProgressConnections, _maintenanceHelperCancellationToken);
                            if (IsRunning)
                            {
                                EnsureMinSize(_maintenanceHelperCancellationToken);
                            }
                        }
                        catch
                        {
                            // ignore exceptions
                        }
                        _maintenanceExecutingManager.Wait(_maintenanceHelperCancellationToken);
                    }
                }
                catch
                {
                    // ignore exceptions
                }
            }

            private void EnsureMinSize(CancellationToken cancellationToken)
            {
                var minTimeout = TimeSpan.FromMilliseconds(20);

                while (_connectionPool.CreatedCount < _connectionPool._settings.MinConnections && !cancellationToken.IsCancellationRequested)
                {
                    using (var poolAwaiter = _connectionPool._maxConnectionsQueue.CreateAwaiter())
                    {
                        var entered = poolAwaiter.WaitSignaled(minTimeout, cancellationToken);
                        if (!entered)
                        {
                            return;
                        }

                        using (var connectionCreator = new ConnectionCreator(_connectionPool, minTimeout))
                        {
                            var connection = connectionCreator.CreateOpened(cancellationToken);
                            _connectionPool._connectionHolder.Return(connection);
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        internal sealed class MaintenanceExecutingManager : IDisposable
        {
            private bool _closeInProgressConnections;
            private AttemptDelay _maintenanceDelay;
            private readonly object _lock = new object();
            private readonly TimeSpan _interval;
            private int _attemptsAfterCancellingRequested;

            public MaintenanceExecutingManager(TimeSpan interval)
            {
                _interval = interval;
                _maintenanceDelay = new AttemptDelay(_interval, TimeSpan.Zero);
            }

            public bool CloseInProgressConnections => _closeInProgressConnections;
            public bool StopLoop => _attemptsAfterCancellingRequested > 1;

            public void Refresh()
            {
                lock (_lock)
                {
                    _maintenanceDelay = new AttemptDelay(_interval, TimeSpan.Zero);
                    _closeInProgressConnections = false;
                }
            }

            public void RequestNextAttempt(bool closeInProgressConnections)
            {
                lock (_lock)
                {
                    _closeInProgressConnections = closeInProgressConnections;
                    _maintenanceDelay?.RequestNextAttempt();
                }
            }

            public void Wait(CancellationToken cancellationToken)
            {
                bool withWaiting = true;
                AttemptDelay newMaintenanceDelay;
                lock (_lock)
                {
                    _closeInProgressConnections = false;

                    var earlyAttemptHasBeenRequested = _maintenanceDelay.EarlyAttemptHasBeenRequested;
                    newMaintenanceDelay = new AttemptDelay(_interval, TimeSpan.Zero);

                    _maintenanceDelay.Dispose();
                    _maintenanceDelay = newMaintenanceDelay;

                    if (earlyAttemptHasBeenRequested)
                    {
                        withWaiting = false;
                    }
                }
                if (withWaiting)
                {
                    newMaintenanceDelay.Wait(cancellationToken);
                }

                if (newMaintenanceDelay.EarlyAttemptHasBeenRequested)
                {
                    Interlocked.Increment(ref _attemptsAfterCancellingRequested);
                }
            }

            public void Dispose()
            {
                _maintenanceDelay?.Dispose();
            }
        }
    }
}
