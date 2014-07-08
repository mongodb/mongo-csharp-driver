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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.ConnectionPools
{
    /// <summary>
    /// Represents a connection that has been acquired from a pool. Calling Dispose will return it to the pool.
    /// </summary>
    internal class AcquiredConnection : ConnectionWrapper
    {
        // fields
        private readonly PooledConnection _pooledConnection;

        // constructors
        public AcquiredConnection(PooledConnection wrapped)
            : base(wrapped)
        {
            _pooledConnection = Ensure.IsNotNull(wrapped, "wrapped");
            _pooledConnection.IncrementReferenceCount();
        }

        // methods
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!Disposed)
                {
                    _pooledConnection.DecrementReferenceCount();
                }
            }
            Disposed = true;
            // do not call base.Dispose, we only want to decrement the reference count
        }

        protected override IConnection Fork()
        {
            return new AcquiredConnection(_pooledConnection);
        }
    }
}
