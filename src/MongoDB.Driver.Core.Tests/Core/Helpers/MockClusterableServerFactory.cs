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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using NSubstitute;

namespace MongoDB.Driver.Core.Helpers
{
    public class MockClusterableServerFactory : IClusterableServerFactory
    {
        private readonly Dictionary<EndPoint, ServerTuple> _servers;
        private readonly IEventSubscriber _eventSubscriber;

        public MockClusterableServerFactory(IEventSubscriber eventSubscriber = null)
        {
            _servers = new Dictionary<EndPoint, ServerTuple>();
            _eventSubscriber = eventSubscriber;
        }

        public IClusterableServer CreateServer(ClusterId clusterId, EndPoint endPoint)
        {
            ServerTuple result;
            if (!_servers.TryGetValue(endPoint, out result))
            {
                var description = new ServerDescription(new ServerId(clusterId, endPoint), endPoint);

                if (_eventSubscriber == null)
                {
                    var server = Substitute.For<IClusterableServer>();
                    server.Description.Returns(description);
                    server.EndPoint.Returns(endPoint);
                    server.ServerId.Returns(new ServerId(clusterId, endPoint));
                    result = new ServerTuple
                    {
                        Server = server
                    };
                }
                else
                {
                    var monitorFactory = Substitute.For<IServerMonitorFactory>();
                    var monitor = Substitute.For<IServerMonitor>();
                    monitorFactory.Create(null, null).ReturnsForAnyArgs(monitor);
                    monitor.Description.Returns(description);

                    result = new ServerTuple
                    {
                        Server = new Server(
                            clusterId,
                            ClusterConnectionMode.Automatic,
                            new ServerSettings(),
                            endPoint,
                            Substitute.For<IConnectionPoolFactory>(),
                            monitorFactory,
                            _eventSubscriber),
                        Monitor = monitor
                    };
                }

                _servers[endPoint] = result;
            }

            return result.Server;
        }

        public IClusterableServer GetServer(EndPoint endPoint)
        {
            ServerTuple result;
            if (!_servers.TryGetValue(endPoint, out result))
            {
                throw new InvalidOperationException("Server does not exist.");
            }

            return result.Server;
        }

        public ServerDescription GetServerDescription(EndPoint endPoint)
        {
            var server = GetServer(endPoint);
            return server.Description;
        }

        public void PublishDescription(ServerDescription description)
        {
            ServerTuple result;
            if (!_servers.TryGetValue(description.EndPoint, out result))
            {
                throw new InvalidOperationException("Server does not exist.");
            }

            var oldDescription = result.Server.Description;

            if (result.Monitor == null)
            {
                result.Server.Description.Returns(description);
                result.Server.DescriptionChanged += Raise.EventWith(new ServerDescriptionChangedEventArgs(oldDescription, description));

            }
            else
            {
                result.Monitor.Description.Returns(description);
                result.Monitor.DescriptionChanged += Raise.EventWith(new ServerDescriptionChangedEventArgs(oldDescription, description));
            }
        }

        private class ServerTuple
        {
            public IClusterableServer Server;
            public IServerMonitor Monitor;
        }
    }
}