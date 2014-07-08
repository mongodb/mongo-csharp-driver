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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    public class ServerReadBinding : ReadBindingHandle
    {
        // constructors
        public ServerReadBinding(IServer server, ReadPreference readPreference)
            : this(new ReferenceCountedReadBinding(new Implementation(server, readPreference)))
        {
        }

        private ServerReadBinding(ReferenceCountedReadBinding wrapped)
            : base(wrapped)
        {
        }

        // methods
        protected override ReadBindingHandle CreateNewHandle(ReferenceCountedReadBinding wrapped)
        {
            return new ServerReadBinding(wrapped);
        }

        // nested types
        internal class Implementation : IReadBinding
        {
            // fields
            private bool _disposed;
            private readonly IServer _server;
            private readonly ReadPreference _readPreference;

            // constructors
            public Implementation(IServer server, ReadPreference readPreference)
            {
                _server = Ensure.IsNotNull(server, "server");
                _readPreference = Ensure.IsNotNull(readPreference, "readPreference");
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

            protected Task<IConnectionSource> GetConnectionSourceAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return Task.FromResult<IConnectionSource>(new ServerConnectionSource(_server));
            }

            public Task<IConnectionSource> GetReadConnectionSourceAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                return GetConnectionSourceAsync(timeout, cancellationToken);
            }

            protected void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }
    }
}
