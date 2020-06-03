/* Copyright 2013-present MongoDB Inc.
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
using System.Net;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using Moq;

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

        public IClusterableServer CreateServer(ClusterId clusterId, IClusterClock clusterClock, EndPoint endPoint)
        {
            ServerTuple result;
            if (!_servers.TryGetValue(endPoint, out result) || result.HasBeenRemoved)
            {
                var serverId = new ServerId(clusterId, endPoint);
                var description = new ServerDescription(serverId, endPoint, reasonChanged: "Initial D");

                if (_eventSubscriber == null)
                {
                    var mockServer = new Mock<IClusterableServer>() { DefaultValue = DefaultValue.Mock };
                    mockServer.SetupGet(s => s.Description).Returns(description);
                    mockServer.SetupGet(s => s.EndPoint).Returns(endPoint);
                    mockServer.SetupGet(s => s.ServerId).Returns(new ServerId(clusterId, endPoint));
                    mockServer
                        .Setup(s => s.Dispose())
                        .Callback(
                            () =>
                            {
                                _servers[mockServer.Object.EndPoint].HasBeenRemoved = true;
                            });
                    result = new ServerTuple
                    {
                        Server = mockServer.Object
                    };
                }
                else
                {
                    var mockMonitorFactory = new Mock<IServerMonitorFactory>();
                    var mockMonitor = new Mock<IServerMonitor>() { DefaultValue = DefaultValue.Mock };
                    mockMonitorFactory.Setup(f => f.Create(It.IsAny<ServerId>(), It.IsAny<EndPoint>())).Returns(mockMonitor.Object);
                    mockMonitor.SetupGet(m => m.Description).Returns(description);
                    mockMonitor
                        .Setup(s => s.Dispose())
                        .Callback(
                            () =>
                            {
                                _servers[mockMonitor.Object.Description.EndPoint].HasBeenRemoved = true;
                            });
                    var mockConnection = new Mock<IConnectionHandle>();
                    var mockConnectionPool = new Mock<IConnectionPool>();
                    var poolGeneration = 0;
                    var connectionGeneration = 0;
                    // need to use a func to close over connectionGeneration
                    mockConnection.Setup(c => c.Generation).Returns(valueFunction: () => connectionGeneration);
                    // need to use a func to close over poolGeneration
                    mockConnectionPool.Setup(p => p.Generation).Returns(valueFunction: () => poolGeneration);
                    Action acquireConnectionCallback = () => { connectionGeneration = poolGeneration; };
                    mockConnectionPool
                        .Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>()))
                        .Callback(acquireConnectionCallback)
                        .Returns(mockConnection.Object);
                    mockConnectionPool
                        .Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>()))
                        .Callback(acquireConnectionCallback)
                        .ReturnsAsync(mockConnection.Object);
                    mockConnectionPool.Setup(p => p.Clear()).Callback(() => { ++poolGeneration; });
                    var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory> { DefaultValue = DefaultValue.Mock };
                    mockConnectionPoolFactory
                        .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), endPoint))
                        .Returns(mockConnectionPool.Object);

                    result = new ServerTuple
                    {
                        Server = new Server(
                            clusterId,
                            clusterClock,
                            ClusterConnectionMode.Automatic,
                            new ServerSettings(),
                            endPoint,
                            mockConnectionPoolFactory.Object,
                            mockMonitorFactory.Object,
                            _eventSubscriber),
                        Monitor = mockMonitor.Object
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
                var mockServer = Mock.Get(result.Server);
                mockServer.SetupGet(s => s.Description).Returns(description);
                mockServer.Raise(s => s.DescriptionChanged += null, new ServerDescriptionChangedEventArgs(oldDescription, description));
            }
            else
            {
                if (description.Version != null || description.WireVersionRange != null)
                {
                    var version = description.Version?.ToString() ??
                        WireVersionHelper.MapWireVersionToServerVersion(description.WireVersionRange.Max);
                    var server = (Server)result.Server;
                    var isMasterResult = new IsMasterResult(new BsonDocument { { "compressors", new BsonArray() } });
                    var buildInfoResult = new BuildInfoResult(new BsonDocument { { "version", version } });
                    var mockConnection = Mock.Get(server._connectionPool().AcquireConnection(CancellationToken.None));
                    mockConnection.SetupGet(c => c.Description)
                        .Returns(new ConnectionDescription(new ConnectionId(description.ServerId, 0), isMasterResult, buildInfoResult));
                }
                var mockMonitor = Mock.Get(result.Monitor);
                mockMonitor.SetupGet(m => m.Description).Returns(description);
                mockMonitor.Raise(m => m.DescriptionChanged += null, new ServerDescriptionChangedEventArgs(oldDescription, description));
            }
        }

        private class ServerTuple
        {
            public bool HasBeenRemoved;
            public IClusterableServer Server;
            public IServerMonitor Monitor;
        }

    }
    internal static class ServerReflector
    {
        public static IConnectionPool _connectionPool(this Server server)
        {
            return (IConnectionPool)Reflector.GetFieldValue(server, nameof(_connectionPool));
        }
    }

    internal static class WireVersionHelper
    {
        public static string MapWireVersionToServerVersion(int wireVersion)
        {
            // loose mapping via https://github.com/mongodb/specifications/blob/master/source/wireversion-featurelist.rst
            switch (wireVersion)
            {
                case 0:
                case 1:
                case 2: return "2.6.0";
                case 3: return "3.0.0";
                case 4: return "3.2.0";
                case 5: return "3.4.0";
                case 6: return "3.6.0";
                case 7: return "4.0.0";
                case 8: return "4.2.0";
                case 9: return "4.4.0";
                case 1000: return "9001.0.0";
                default: throw new ArgumentException($"Unknown wire version: {wireVersion}");
            }
        }
    }
}
