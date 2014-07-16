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
using MongoDB.Driver.Core.Clusters.Events;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a standalone cluster.
    /// </summary>
    public class StandaloneCluster : MultiServerCluster
    {
        // constructor
        internal StandaloneCluster(ClusterSettings settings, IServerFactory serverFactory, IClusterListener listener)
            : base(settings, serverFactory, listener)
        {
            if (settings.ClusterType != ClusterType.Standalone) { throw new ArgumentException(string.Format("Invalid cluster type: {0}.", settings.ClusterType), "settings"); }
        }
    }
}
