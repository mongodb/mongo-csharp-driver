/* Copyright 2010-2013 10gen Inc.
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
using System.Collections.ObjectModel;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Represents the type of an IMongoServerProxy.
    /// </summary>
    internal enum MongoServerProxyType
    {
        /// <summary>
        /// The type of the proxy is not yet known.
        /// </summary>
        Unknown,
        /// <summary>
        /// A proxy to a single node.
        /// </summary>
        Direct,
        /// <summary>
        /// A proxy to a replica set.
        /// </summary>
        ReplicaSet,
        /// <summary>
        /// A proxy to a sharded router.
        /// </summary>
        Sharded
    }
}