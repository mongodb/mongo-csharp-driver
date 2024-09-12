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
using static MongoDB.Driver.Core.ConnectionPools.ExclusiveConnectionPool;

namespace MongoDB.Driver.Core.ConnectionPools
{
    // Not thread safe class. Start and Stop MUST be synchronized.
    internal sealed class MaintenanceHelper : IDisposable
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly ExclusiveConnectionPool _connectionPool;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly CancellationToken _globalCancellationToken;
        private readonly CancellationTokenSource _globalCancellationTokenSource;
        private readonly TimeSpan _interval;
        private MaintenanceExecutingContext _maintenanceExecutingContext;
        private Thread _maintenanceThread;

        public MaintenanceHelper(ExclusiveConnectionPool connectionPool, TimeSpan interval)
        {
            _connectionPool = Ensure.IsNotNull(connectionPool, nameof(connectionPool));
            _globalCancellationTokenSource = new CancellationTokenSource();
            _globalCancellationToken = _globalCancellationTokenSource.Token;
            _interval = interval;
        }

        public bool IsRunning => _maintenanceThread != null;

        public void Stop(int? maxGenerationToReap)
        {
            if (_interval == Timeout.InfiniteTimeSpan || !IsRunning)
            {
                return;
            }

            _maintenanceThread = null;

            _maintenanceExecutingContext.Cancel(maxGenerationToReap);
            _maintenanceExecutingContext.Dispose();
        }

        public void Start()
        {
            if (_interval == Timeout.InfiniteTimeSpan || IsRunning)
            {
                return;
            }

            _maintenanceExecutingContext = new MaintenanceExecutingContext(_interval, _globalCancellationToken);

            _maintenanceThread = new Thread(new ParameterizedThreadStart(ThreadStart)) { IsBackground = true };
            _maintenanceThread.Start(_maintenanceExecutingContext);

            void ThreadStart(object maintenanceExecutingContextObj)
            {
                var maintenanceExecutingContext = (MaintenanceExecutingContext)maintenanceExecutingContextObj;

                RunMaintenance(maintenanceExecutingContext);
            }
        }

        public void Dispose()
        {
            _maintenanceExecutingContext?.Dispose();
            _globalCancellationTokenSource.Cancel();
            _globalCancellationTokenSource.Dispose();
        }

        // private methods
        private void RunMaintenance(MaintenanceExecutingContext maintenanceExecutingContext)
        {
            try
            {
                var cancellationToken = maintenanceExecutingContext.CancellationToken;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        _connectionPool.ConnectionHolder.Prune(maxExpiredGenerationInUse: null, cancellationToken);
                        EnsureMinSize(cancellationToken);
                    }
                    catch
                    {
                    }

                    maintenanceExecutingContext.Wait();
                }

                _connectionPool.ConnectionHolder.Prune(maintenanceExecutingContext.MaxGenerationToReap, _globalCancellationToken);
            }
            catch
            {
                // ignore exceptions
            }
        }

        private void EnsureMinSize(CancellationToken cancellationToken)
        {
            var minTimeout = TimeSpan.FromMilliseconds(20);

            while (_connectionPool.CreatedCount < _connectionPool.Settings.MinConnections && !cancellationToken.IsCancellationRequested)
            {
                using (var poolAwaiter = _connectionPool.CreateMaxConnectionsAwaiter())
                {
                    var entered = poolAwaiter.WaitSignaled(minTimeout, cancellationToken);
                    if (!entered)
                    {
                        return;
                    }

                    using (var connectionCreator = new ConnectionCreator(_connectionPool, minTimeout))
                    {
                        var connection = connectionCreator.CreateOpened(cancellationToken);
                        _connectionPool.ConnectionHolder.Return(connection);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

    internal sealed class MaintenanceExecutingContext : IDisposable
    {
        private readonly AutoResetEvent _autoResetEvent;
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly TimeSpan _interval;
        private int? _maxGenerationToReap;

        public MaintenanceExecutingContext(TimeSpan interval, CancellationToken globalCancellationToken)
        {
            _autoResetEvent = new AutoResetEvent(initialState: false);
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(globalCancellationToken);
            _cancellationToken = _cancellationTokenSource.Token;
            _interval = interval;
        }

        // public properties
        public CancellationToken CancellationToken => _cancellationToken;
        public int? MaxGenerationToReap => _maxGenerationToReap;

        // public methods
        public void Dispose()
        {
            Cancel(_maxGenerationToReap); // stop waiting if any
            _cancellationTokenSource.Dispose();
            _autoResetEvent.Dispose();
        }

        public void Cancel(int? maxGenerationToReap)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _maxGenerationToReap = maxGenerationToReap;

            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // might be already cancelled and disposed in Dispose, ignore it
            }

            try
            {
                _autoResetEvent.Set();
            }
            catch (ObjectDisposedException)
            {
                // ignore it
            }
        }

        public void Wait()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                _autoResetEvent.WaitOne(_interval);
            }
            catch (ObjectDisposedException)
            {
                // ignore it
            }
        }
    }
}
