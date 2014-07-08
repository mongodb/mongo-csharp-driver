/* Copyright 2013-2014 MongoDB Inc.
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
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a MongoDB cluster.
    /// </summary>
    public class ClusterWrapper : ICluster
    {
        // events
        public event EventHandler DescriptionChanged;

        // fields
        private bool _disposed;
        private readonly object _lock = new object();
        private readonly bool _ownsWrapped;
        private readonly ICluster _wrapped;

        // constructors
        public ClusterWrapper(ICluster wrapped)
            : this(wrapped, true)
        {
        }

        public ClusterWrapper(ICluster wrapped, bool ownsWrapped)
        {
            _wrapped = wrapped;
            _wrapped.DescriptionChanged += OnDescriptionChanged;
            _ownsWrapped = ownsWrapped;
        }

        // properties
        public virtual ClusterDescription Description
        {
            get { return _wrapped.Description; }
        }

        public virtual ClusterSettings Settings
        {
            get { return _wrapped.Settings; }

        }
        
        public virtual ICluster Wrapped
        {
            get { return _wrapped; }
        }

        // methods
        public void Dispose()
        {
            lock (_lock)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _wrapped.DescriptionChanged -= OnDescriptionChanged;
                    if (_ownsWrapped)
                    {
                        _wrapped.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public virtual Task<ClusterDescription> GetDescriptionAsync(int minimumRevision = 0, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return _wrapped.GetDescriptionAsync(minimumRevision, timeout, cancellationToken);
        }

        public virtual IServer GetServer(DnsEndPoint endPoint)
        {
            ThrowIfDisposed();
            return _wrapped.GetServer(endPoint);
        }

        private void OnDescriptionChanged(object sender, EventArgs args)
        {
            var handler = DescriptionChanged;
            if (handler != null)
            {
                try
                {
                    handler(this, args);
                }
                catch
                {
                    // ignore exceptions in event handler
                }
            }
        }

        public virtual Task<IServer> SelectServerAsync(IServerSelector selector = null, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return _wrapped.SelectServerAsync(selector, timeout, cancellationToken);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }

    public class ClusterHandle : ClusterWrapper
    {
        // fields
        private readonly ReferenceCountedCluster _referenceCountedCluster;

        // constructors
        public ClusterHandle(ICluster cluster)
            : this(cluster, null)
        {
        }

        public ClusterHandle(ICluster cluster, Action disposedCallback)
            : this(new ReferenceCountedCluster(cluster, disposedCallback))
        {
        }

        private ClusterHandle(ReferenceCountedCluster referenceCountedCluster)
            : base(referenceCountedCluster, ownsWrapped: false)
        {
            _referenceCountedCluster = referenceCountedCluster;
        }

        // properties
        public int ReferenceCount
        {
            get { return _referenceCountedCluster.ReferenceCount; }
        }

        // methods
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _referenceCountedCluster.DecrementReferenceCount();
            }
            base.Dispose(disposing);
        }

        public ClusterHandle Fork()
        {
            ThrowIfDisposed();
            _referenceCountedCluster.IncrementReferenceCount();
            return new ClusterHandle(_referenceCountedCluster);
        }
    }

    public class ReferenceCountedCluster : ClusterWrapper
    {
        // fields
        private Action _disposedCallback;
        private int _referenceCount = 1;

        // constructors
        public ReferenceCountedCluster(ICluster wrapped)
            : this(wrapped, null)
        {
        }

        public ReferenceCountedCluster(ICluster wrapped, Action disposedCallback)
            : base(wrapped)
        {
            _disposedCallback = disposedCallback;
        }

        // properties
        public int ReferenceCount
        {
            get { return _referenceCount; }
        }

        // methods
        public int DecrementReferenceCount()
        {
            var referenceCount = Interlocked.Decrement(ref _referenceCount);
            if (referenceCount == 0)
            {
                if (_disposedCallback != null)
                {
                    _disposedCallback();
                }
                Dispose();
            }
            return referenceCount;
        }

        public int IncrementReferenceCount()
        {
            return Interlocked.Increment(ref _referenceCount);
        }
    }
}
