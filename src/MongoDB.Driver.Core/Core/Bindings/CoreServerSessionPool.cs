/* Copyright 2017-present MongoDB Inc.
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
using System.Collections.Generic;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal class CoreServerSessionPool : ICoreServerSessionPool
    {
        // private fields
        private readonly ICluster _cluster;
        private readonly ConcurrentQueue<ICoreServerSession> _pool = new ConcurrentQueue<ICoreServerSession>();

        // constructors
        public CoreServerSessionPool(ICluster cluster)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
        }

        /// <inheritdoc />
        public ICoreServerSession AcquireSession()
        {
            // try to find first non-expired session in our FIFO buffer
            // if none found - create new session

            ICoreServerSession pooledSession;
            while (_pool.TryDequeue(out pooledSession))
            {
                if (IsAboutToExpire(pooledSession))
                {
                    pooledSession.Dispose();
                }
                else
                {
                    return new ReleaseOnDisposeCoreServerSession(pooledSession, this);
                }
            }

            return new ReleaseOnDisposeCoreServerSession(new CoreServerSession(), this);
        }

        /// <inheritdoc />
        public void ReleaseSession(ICoreServerSession session)
        {
            // if session is not expired - put it in the FIFO pool

            if (IsAboutToExpire(session))
            {
                session.Dispose();
            }
            else
            {
                _pool.Enqueue(session);
            }
        }

        // private methods
        private bool IsAboutToExpire(ICoreServerSession session)
        {
            var logicalSessionTimeout = _cluster.Description.LogicalSessionTimeout;
            if (!session.LastUsedAt.HasValue || !logicalSessionTimeout.HasValue)
            {
                return true;
            }
            else
            {
                var expiresAt = session.LastUsedAt.Value + logicalSessionTimeout.Value;
                var timeRemaining = expiresAt - DateTime.UtcNow;
                return timeRemaining < TimeSpan.FromMinutes(1);
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
