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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors;

internal sealed class DeprioritizedServersServerSelector : IServerSelector
{
    private readonly IReadOnlyCollection<EndPoint> _deprioritizedServers;
    private readonly IServerSelector _wrappedServerSelector;

    public DeprioritizedServersServerSelector(IReadOnlyCollection<ServerDescription> deprioritizedServers, IServerSelector wrappedServerSelector)
    {
        _deprioritizedServers = Ensure.IsNotNull(deprioritizedServers, nameof(deprioritizedServers)).Select(s => s.EndPoint).ToArray();
        _wrappedServerSelector = Ensure.IsNotNull(wrappedServerSelector, nameof(wrappedServerSelector));
    }

    public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
    {
        var result = servers.Where(s => !_deprioritizedServers.Contains(s.EndPoint));
        result = _wrappedServerSelector.SelectServers(cluster, result);

        if (!result.Any())
        {
            result = _wrappedServerSelector.SelectServers(cluster, servers);
        }

        return result;
    }

    public override string ToString() => $"DeprioritizedServersServerSelector{{{{ Deprioritized servers: {string.Join(", ", _deprioritizedServers)} }}, WrappedServerSelector: {_wrappedServerSelector}}}";
}

