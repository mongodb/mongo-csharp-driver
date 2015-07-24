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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using NSubstitute;

namespace MongoDB.Driver.Core.Helpers
{
    public class MockClusterableServerFactory : IClusterableServerFactory
    {
        private Dictionary<EndPoint, IClusterableServer> _servers = new Dictionary<EndPoint, IClusterableServer>();

        public IClusterableServer CreateServer(ClusterId clusterId, EndPoint endPoint)
        {
            IClusterableServer result;
            if(!_servers.TryGetValue(endPoint, out result))
            {
                var description = new ServerDescription(new ServerId(clusterId, endPoint), endPoint);
                _servers[endPoint] = result = Substitute.For<IClusterableServer>();
                result.Description.Returns(description);
                result.EndPoint.Returns(endPoint);
                result.ServerId.Returns(new ServerId(clusterId, endPoint));
            }

            return result;
        }

        public IClusterableServer GetServer(EndPoint endPoint)
        {
            IClusterableServer result;
            if (!_servers.TryGetValue(endPoint, out result))
            {
                throw new InvalidOperationException("Server does not exist.");
            }

            return result;
        }

        public ServerDescription GetServerDescription(EndPoint endPoint)
        {
            var server = GetServer(endPoint);
            return server.Description;
        }

        public void PublishDescription(ServerDescription description)
        {
            var server = GetServer(description.EndPoint);

            var oldDescription = server.Description;
            server.Description.Returns(description);

            server.DescriptionChanged += Raise.EventWith(new ServerDescriptionChangedEventArgs(oldDescription, description));
        }
    }
}