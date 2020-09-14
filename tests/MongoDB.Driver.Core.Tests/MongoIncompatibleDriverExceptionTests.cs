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

using System.IO;
using System.Net;
#if NET452
using System.Runtime.Serialization.Formatters.Binary;
#endif
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoIncompatibleDriverExceptionTests
    {
        private ClusterDescription _clusterDescription;

        public MongoIncompatibleDriverExceptionTests()
        {
            var clusterId = new ClusterId(1);
#pragma warning disable CS0618 // Type or member is obsolete
            var connectionMode = ClusterConnectionMode.Standalone;
#pragma warning restore CS0618 // Type or member is obsolete
            var clusterType = ClusterType.Standalone;
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var server = new ServerDescription(serverId, endPoint, wireVersionRange: new Range<int>(0, 0), type: ServerType.Standalone);
            var servers = new[] { server };
#pragma warning disable CS0618 // Type or member is obsolete
            _clusterDescription = new ClusterDescription(clusterId, connectionMode, clusterType, servers);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void constructor_should_initalize_subject()
        {
            var subject = new MongoIncompatibleDriverException(_clusterDescription);

            subject.Message.Should().StartWith("Server at localhost:27017 reports wire version 0");
            subject.InnerException.Should().BeNull();
        }

#if NET452
        [Fact]
        public void Serialization_should_work()
        {
            var subject = new MongoIncompatibleDriverException(_clusterDescription);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoIncompatibleDriverException)formatter.Deserialize(stream);

                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.InnerException.Should().BeNull();
            }
        }
#endif
    }
}
