/* Copyright 2013-2015 MongoDB Inc.
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
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver
{
    [TestFixture]
    public class MongoIncompatibleDriverExceptionTests
    {
        private ClusterDescription _clusterDescription;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            var clusterId = new ClusterId(1);
            var connectionMode = ClusterConnectionMode.Standalone;
            var clusterType = ClusterType.Standalone;
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var server = new ServerDescription(serverId, endPoint);
            var servers = new[] { server };
            _clusterDescription = new ClusterDescription(clusterId, connectionMode, clusterType, servers);
        }

        [Test]
        public void constructor_should_initalize_subject()
        {
            var subject = new MongoIncompatibleDriverException(_clusterDescription);

            subject.Message.StartsWith("This version of the driver is not compatible");
            subject.Message.EndsWith(":" + _clusterDescription.ToString() + ".");
            subject.InnerException.Should().BeNull();
        }

        [Test]
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
    }
}
