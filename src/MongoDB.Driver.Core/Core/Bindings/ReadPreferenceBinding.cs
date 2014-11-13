﻿/* Copyright 2013-2014 MongoDB Inc.
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
    public sealed class ReadPreferenceBinding : IReadBinding
    {
        // fields
        private readonly ICluster _cluster;
        private bool _disposed;
        private readonly ReadPreference _readPreference;
        private readonly IServerSelector _serverSelector;

        // constructors
        public ReadPreferenceBinding(ICluster cluster, ReadPreference readPreference)
        {
            _cluster = Ensure.IsNotNull(cluster, "cluster");
            _readPreference = Ensure.IsNotNull(readPreference, "readPreference");
            _serverSelector = new ReadPreferenceServerSelector(readPreference);
        }

        // properties
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
        }

        // methods
        public async Task<IConnectionSourceHandle> GetReadConnectionSourceAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var server = await _cluster.SelectServerAsync(_serverSelector, cancellationToken).ConfigureAwait(false);
            return new ConnectionSourceHandle(new ServerConnectionSource(server));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
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
