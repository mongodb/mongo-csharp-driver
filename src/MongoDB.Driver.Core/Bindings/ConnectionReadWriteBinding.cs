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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Bindings
{
    public class ConnectionReadWriteBinding : ReadWriteBindingHandle
    {
        // constructors
        public ConnectionReadWriteBinding(IServer server, IConnectionHandle connection)
            : this(new ReferenceCountedReadWriteBinding(new Implementation(server, connection)))
        {
        }

        private ConnectionReadWriteBinding(ReferenceCountedReadWriteBinding wrapped)
            : base(wrapped)
        {
        }

        // methods
        protected override ReadBindingHandle CreateNewHandle(ReferenceCountedReadBinding wrapped)
        {
            return new ConnectionReadWriteBinding((ReferenceCountedReadWriteBinding)wrapped);
        }

        // nested types
        private class Implementation : ConnectionReadBinding.Implementation, IReadWriteBinding
        {
            // constructors
            public Implementation(IServer server, IConnectionHandle connection)
                : base(server, connection, ReadPreference.Primary)
            {
            }

            // methods
            IWriteBinding IWriteBinding.Fork()
            {
                throw new NotSupportedException(); // implemented by the handle
            }

            public new IReadWriteBinding Fork()
            {
                throw new NotSupportedException(); // implemented by the handle
            }

            public Task<IConnectionSource> GetWriteConnectionSourceAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                return GetConnectionSourceAsync(timeout, cancellationToken);
            }
        }
    }
}
