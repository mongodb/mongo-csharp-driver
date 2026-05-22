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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver
{
    internal sealed class CoreServerSessionPool : ICoreServerSessionPool
    {
        // private fields
        private readonly ICluster _cluster;
        private readonly ILogger<LogCategories.Client> _logger;
        private readonly ConcurrentStack<ICoreServerSession> _pool = new();
        private bool _isDisposed = false;
        private long _sessionsCreated;
        private long _sessionsReturned;

        // constructors
        public CoreServerSessionPool(ICluster cluster, ILogger<LogCategories.Client> logger)
        {
            _logger = logger;
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
        }

        public ICoreServerSession AcquireSession()
        {
            ThrowIfDisposed();
            while (_pool.TryPop(out var pooledSession))
            {
                if (IsAboutToExpireOrDirty(pooledSession))
                {
                    pooledSession.Dispose();
                }
                else
                {
                    Interlocked.Increment(ref _sessionsCreated);
                    return new ReleaseOnDisposeCoreServerSession(pooledSession, this);
                }
            }

            Interlocked.Increment(ref _sessionsCreated);
            return new ReleaseOnDisposeCoreServerSession(new CoreServerSession(), this);
        }

        public void ReleaseSession(ICoreServerSession session)
        {
            ThrowIfDisposed();
            Interlocked.Increment(ref _sessionsReturned);

            if (IsAboutToExpireOrDirty(session))
            {
                session.Dispose();
            }
            else
            {
                _pool.Push(session);
            }
        }

        public void CloseAndDispose(IServer server)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            var timestamp = Stopwatch.GetTimestamp();
            _logger?.LogDebug(
                "Closing server session pool for {clusterId}: total sessions created {sessionsCreated}, sessions returned {sessionsReturned}, pooled sessions {pooledSessions}.",
                _cluster.ClusterId, _sessionsCreated, _sessionsReturned, _pool.Count);
            try
            {
                while (true)
                {
                    var batchSize = Math.Min(10000, _pool.Count);
                    var batch = new ICoreServerSession[batchSize];

                    batchSize = _pool.TryPopRange(batch);
                    if(batchSize == 0)
                    {
                        return;
                    }

                    var endSessionCommand = new BsonDocument("endSessions", new BsonArray(batch.Take(batchSize).Select(s => s.Id)));
                    var operationContext = OperationContext.NoTimeout;
                    using var channel = server.GetChannel(operationContext);
                    channel.Command(
                        operationContext,
                        NoCoreSession.Instance,
                        ReadPreference.PrimaryPreferred,
                        DatabaseNamespace.Admin,
                        endSessionCommand,
                        null,
                        NoOpElementNameValidator.Instance,
                        null,
                        null,
                        CommandResponseHandling.Return,
                        BsonDocumentSerializer.Instance,
                        null);
                }
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, "Error closing server session pool for {clusterId}: {exception}", _cluster.ClusterId, ex);
            }
            finally
            {
                _logger?.LogDebug(
                    "Closed server session pool for {clusterId} in {milliseconds}ms.",
                    _cluster.ClusterId, (Stopwatch.GetTimestamp() - timestamp) / (double)Stopwatch.Frequency * 1000);
            }
        }

        // private methods
        private bool IsAboutToExpire(ICoreServerSession session)
        {
            var logicalSessionTimeout = _cluster.Description.LogicalSessionTimeout;
            var clusterType = _cluster.Description.Type;

            if (clusterType == ClusterType.LoadBalanced)
            {
                return false;  // sessions never expire in load balancing mode
            }
            else if (session.LastUsedAt.HasValue && logicalSessionTimeout.HasValue)
            {
                var expiresAt = session.LastUsedAt.Value + logicalSessionTimeout.Value;
                var timeRemaining = expiresAt - DateTime.UtcNow;
                return timeRemaining < TimeSpan.FromMinutes(1);
            }
            else
            {
                return true;
            }
        }

        private bool IsAboutToExpireOrDirty(ICoreServerSession session)
        {
            return IsAboutToExpire(session) || session.IsDirty;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(CoreServerSessionPool));
            }
        }

        // nested types
        internal sealed class ReleaseOnDisposeCoreServerSession : WrappingCoreServerSession
        {
            // private fields
            private readonly ICoreServerSessionPool _pool;

            // constructors
            public ReleaseOnDisposeCoreServerSession(ICoreServerSession wrapped, ICoreServerSessionPool pool)
                : base(wrapped, ownsWrapped: false)
            {
                _pool = Ensure.IsNotNull(pool, nameof(pool));
            }

            // protected methods
            protected override void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _pool.ReleaseSession(Wrapped);
                    }
                }
                base.Dispose(disposing);
            }
        }
    }
}
