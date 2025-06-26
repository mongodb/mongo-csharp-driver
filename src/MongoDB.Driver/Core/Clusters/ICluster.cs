/* Copyright 2010-present MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a MongoDB cluster.
    /// </summary>
    public interface ICluster : IDisposable
    {
        // properties
        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        /// <value>
        /// The cluster identifier.
        /// </value>
        ClusterId ClusterId { get; }

        /// <summary>
        /// Gets the cluster description.
        /// </summary>
        /// <value>
        /// The cluster description.
        /// </value>
        ClusterDescription Description { get; }

        /// <summary>
        /// Gets the cluster settings.
        /// </summary>
        /// <value>
        /// The cluster settings.
        /// </value>
        ClusterSettings Settings { get; }
    }

    internal interface IClusterInternal : ICluster
    {
        event EventHandler<ClusterDescriptionChangedEventArgs> DescriptionChanged;

        ICoreServerSession AcquireServerSession();

        void Initialize();

        (IServer, TimeSpan) SelectServer(OperationContext operationContext, IServerSelector selector);
        Task<(IServer, TimeSpan)> SelectServerAsync(OperationContext operationContext, IServerSelector selector);

        ICoreSessionHandle StartSession(CoreSessionOptions options = null);
    }
}
