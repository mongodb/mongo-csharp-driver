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

namespace MongoDB.Driver.Core.Bindings
{
    public class ConnectionSourceReadWriteBinding : ReadWriteBindingHandle
    {
        // constructors
        public ConnectionSourceReadWriteBinding(IConnectionSource connectionSource)
            : base(new ReferenceCountedReadWriteBinding(new Implementation(connectionSource)))
        {
        }

        private ConnectionSourceReadWriteBinding(ReferenceCountedReadWriteBinding wrapped)
            : base(wrapped)
        {
        }

        // methods
        protected override ReadBindingHandle CreateNewHandle(ReferenceCountedReadBinding wrapped)
        {
            return new ConnectionSourceReadWriteBinding((ReferenceCountedReadWriteBinding)wrapped);
        }

        // nested types
        private class Implementation : ConnectionSourceReadBinding.Implementation, IReadWriteBinding
        {
            // constructors
            public Implementation(IConnectionSource connectionSource)
                : base(connectionSource, ReadPreference.Primary)
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
