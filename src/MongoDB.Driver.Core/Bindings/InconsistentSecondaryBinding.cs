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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Bindings
{
    public class InconsistentSecondaryBinding : ReadBindingHandle
    {
        // constructors
        public InconsistentSecondaryBinding(ICluster cluster, ReadPreference readPreference)
            : this(new ReferenceCountedReadBinding(new Implementation(cluster, readPreference)))
        {
        }

        private InconsistentSecondaryBinding(ReferenceCountedReadBinding wrapped)
            : base(wrapped)
        {
        }

        // methods
        protected override ReadBindingHandle CreateNewHandle(ReferenceCountedReadBinding wrapped)
        {
            return new InconsistentSecondaryBinding(wrapped);
        }

        // nested types
        private class Implementation : IReadBinding
        {
            // fields
            private readonly ICluster _cluster;
            private bool _disposed;
            private readonly ReadPreference _readPreference;
            private readonly IServerSelector _selector;

            // constructors
            public Implementation(ICluster cluster, ReadPreference readPreference)
            {
                _cluster = Ensure.IsNotNull(cluster, "cluster");
                _readPreference = Ensure.IsNotNull(readPreference, "readPreference");
                _selector = new ReadPreferenceServerSelector(readPreference);
            }

            // properties
            public ReadPreference ReadPreference
            {
                get { return _readPreference; }
            }

            // methods
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                _disposed = true;
            }

            public IReadBinding Fork()
            {
                throw new NotSupportedException(); // implemented by the handle
            }

            public async Task<IConnectionSource> GetReadConnectionSourceAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                ThrowIfDisposed();
                var secondary = await _cluster.SelectServerAsync(_selector, timeout, cancellationToken);
                return new ServerConnectionSource(secondary);
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }
    }
}
