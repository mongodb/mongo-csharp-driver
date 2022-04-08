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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal sealed partial class ExclusiveConnectionPool
    {
        internal sealed class MaintenanceHelper : IDisposable
        {
            private readonly ExclusiveConnectionPool _connectionPool;
            private readonly TimeSpan _interval;
            private MaintenanceExecutingManager _maintenanceExecutingManager;
            private Thread _maintenanceThread;

            public MaintenanceHelper(ExclusiveConnectionPool connectionPool, TimeSpan interval)
            {
                _connectionPool = Ensure.IsNotNull(connectionPool, nameof(connectionPool));
                _interval = interval;
                _maintenanceExecutingManager = new MaintenanceExecutingManager(_interval); // no op until Start
            }

            public bool IsRunning => _maintenanceThread != null;

            public void RequestStoppingMaintenance(bool closeInUseConnections)
            {
                if (_interval == Timeout.InfiniteTimeSpan)
                {
                    return;
                }

                _maintenanceThread = null;

                _maintenanceExecutingManager.RequestCancelling(closeInUseConnections); // might be no op if Start hasn't been called yet
            }

            public void Start()
            {
                if (_interval == Timeout.InfiniteTimeSpan)
                {
                    return;
                }

                var newMaintenanceExecutingManager = new MaintenanceExecutingManager(_interval);
                var oldMaintenanceExecutingManager = Interlocked.CompareExchange(ref _maintenanceExecutingManager, newMaintenanceExecutingManager, _maintenanceExecutingManager);
                oldMaintenanceExecutingManager.Dispose();

                _maintenanceThread = new Thread(new ParameterizedThreadStart(ThreadStart)) { IsBackground = true };
                _maintenanceThread.Start(newMaintenanceExecutingManager);

                void ThreadStart(object maintenanceExecutingManagerObj)
                {
                    var maintenanceExecutingManager = (MaintenanceExecutingManager)maintenanceExecutingManagerObj;

                    MaintainSize(maintenanceExecutingManager);
                }
            }

            public void Dispose()
            {
                _maintenanceExecutingManager.Dispose();
            }

            // private methods
            private void MaintainSize(MaintenanceExecutingManager maintenanceExecutingManager)
            {
                bool shouldStopLoop;
                bool stopAfterNextIteration = false;

                try
                {
                    do
                    {
                        shouldStopLoop = stopAfterNextIteration;
                        try
                        {
                            _connectionPool._connectionHolder.Prune(maintenanceExecutingManager.CloseInUseConnections, maintenanceExecutingManager.CancellationToken);
                            if (IsRunning)
                            {
                                EnsureMinSize(maintenanceExecutingManager.CancellationToken);
                            }
                        }
                        catch
                        {
                            // ignore exceptions
                        }
                        stopAfterNextIteration = maintenanceExecutingManager.WaitAndSetStop(ignoreWaiting: shouldStopLoop);
                    }
                    while (!maintenanceExecutingManager.CancellationToken.IsCancellationRequested && !shouldStopLoop);
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

        internal class MaintenanceExecutingManager : IDisposable
        {
            private readonly AutoResetEvent _autoResetEvent;
            private readonly CancellationToken _cancellationToken;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private bool _closeInUseConnections;
            private readonly TimeSpan _interval;

            public MaintenanceExecutingManager(TimeSpan interval)
            {
                _autoResetEvent = new AutoResetEvent(initialState: false);
                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;
                _interval = interval;
            }

            // public properties
            public CancellationToken CancellationToken => _cancellationToken;
            public bool CloseInUseConnections => _closeInUseConnections;

            // public methods
            public void Dispose()
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();

                RequestCancelling(_closeInUseConnections); // stop waiting if any
                _autoResetEvent.Dispose();
            }

            public void RequestCancelling(bool closeInUseConnections)
            {
                _closeInUseConnections = closeInUseConnections;

                try
                {
                    _autoResetEvent.Set();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            public bool WaitAndSetStop(bool ignoreWaiting)
            {
                if (ignoreWaiting)
                {
                    return true;
                }

                try
                {
                    return _autoResetEvent.WaitOne(_interval);
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
            }
        }
    }
}
