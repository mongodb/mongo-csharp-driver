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

using MongoDB.Driver.Core.Clusters;

namespace MongoDB.Driver
{
    internal interface IClusterSource
    {
        public IClusterInternal Get(ClusterKey key);
        public void Return(IClusterInternal cluster);
    }

    internal sealed class DefaultClusterSource : IClusterSource
    {
        public static IClusterSource Instance { get; } = new DefaultClusterSource();

        public IClusterInternal Get(ClusterKey key) => ClusterRegistry.Instance.GetOrCreateCluster(key);
        public void Return(IClusterInternal cluster) { /* Do nothing for now, until cluster caching can handle disposing */ }
    }

    internal sealed class DisposingClusterSource : IClusterSource
    {
        public static IClusterSource Instance { get; } = new DisposingClusterSource();

        public IClusterInternal Get(ClusterKey key) => ClusterRegistry.Instance.GetOrCreateCluster(key);
        public void Return(IClusterInternal cluster) => ClusterRegistry.Instance.UnregisterAndDisposeCluster(cluster);
    }
}
