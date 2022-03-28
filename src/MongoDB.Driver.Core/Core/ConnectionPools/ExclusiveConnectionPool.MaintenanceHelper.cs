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
            private CancellationTokenSource _cancellationTokenSource = null;
            private bool _closeInProgressConnection;
            private readonly ExclusiveConnectionPool _connectionPool;
            private Thread _maintenanceThread;
            private volatile MaintenanceDelayManager _maintenanceDelayManager;

            public MaintenanceHelper(ExclusiveConnectionPool connectionPool, TimeSpan interval)
            {
                _connectionPool = Ensure.IsNotNull(connectionPool, nameof(connectionPool));
                _maintenanceDelayManager = new MaintenanceDelayManager(interval);
            }

            public bool IsRunning => _maintenanceThread != null;

            public void RequestCancel(bool closeInProgressConnection)
            {
                _cancellationTokenSource = null;
                _maintenanceThread = null;

                _closeInProgressConnection = closeInProgressConnection;
                _maintenanceDelayManager.RequestNextAttemptWithCallbackAfter(CancelAndDispose);
                // the previous _maintenanceThread might not be stopped yet, but it will be soon
            }

            public void Start()
            {
                CancelAndDispose();
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                _maintenanceThread = new Thread(new ParameterizedThreadStart(ThreadStart)) { IsBackground = true };
                _maintenanceThread.Start(cancellationToken);

                void ThreadStart(object cancellationToken)
                {
                    MaintainSize((CancellationToken)cancellationToken);
                }
            }

            public void Dispose()
            {
                CancelAndDispose();
                _maintenanceDelayManager.Dispose();
            }

            // private methods
            private void CancelAndDispose()
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }

            private void MaintainSize(CancellationToken cancellationToken)
            {
                _maintenanceDelayManager.Initialize();

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            _connectionPool._connectionHolder.Prune(_closeInProgressConnection, cancellationToken);
                            if (IsRunning)
                            {
                                EnsureMinSize(cancellationToken);
                            }
                        }
                        catch
                        {
                            // ignore exceptions
                        }
                        _maintenanceDelayManager.Wait(cancellationToken);
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

        internal sealed class MaintenanceDelayManager : IDisposable
        {
            private AttemptDelay _maintenanceDelay;
            private readonly object _lock = new object();
            private Metronome _metronome;
            private readonly TimeSpan _interval;
            private readonly TimeSpan _minInterval;
            private bool _skipNextAttemptDelay = false;

            public MaintenanceDelayManager(TimeSpan interval)
            {
                _interval = interval;
                _minInterval = TimeSpan.FromMilliseconds(50);
            }

            public void Initialize()
            {
                _metronome = new Metronome(_interval);
                lock (_lock)
                {
                    _maintenanceDelay = new AttemptDelay(_interval, _minInterval);
                }
            }

            public void RequestNextAttemptWithCallbackAfter(Action callback)
            {
                try
                {
                    lock (_lock)
                    {
                        _skipNextAttemptDelay = true;
                        if (_maintenanceDelay != null) // might not be initialized yet
                        {
                            _maintenanceDelay.Task.ContinueWith((task) => callback);
                            _maintenanceDelay.RequestNextAttempt();
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public void Wait(CancellationToken cancellationToken)
            {
                AttemptDelay newMaintenanceDelay;
                lock (_lock)
                {
                    if (_skipNextAttemptDelay)
                    {
                        _skipNextAttemptDelay = false;
                        return; // do not wait
                    }

                    newMaintenanceDelay = new AttemptDelay(_metronome.GetNextTickDelay(), _minInterval);
                    _maintenanceDelay?.Dispose();
                    _maintenanceDelay = newMaintenanceDelay;
                }
                newMaintenanceDelay.Wait(cancellationToken); // corresponds to wait method in spec
            }

            public void Dispose()
            {
                _maintenanceDelay?.Dispose();
            }
        }
    }
}
