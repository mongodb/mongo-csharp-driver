/* Copyright 2019-present MongoDB Inc.
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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ReadPreferenceOnStandaloneTests
    {
        [Theory]
        [ParameterAttributeData]
        public void ReadPreference_should_not_be_sent_to_standalone_server(
            [Values(false, true)] bool async)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e =>
                e.CommandName.Equals("find") || e.CommandName.Equals("$query"));
            using (var client = CreateDisposableClient(eventCapturer, ReadPreference.PrimaryPreferred))
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                if (async)
                {
                    var _ = collection.FindAsync("{ x : 2 }").GetAwaiter().GetResult();
                }
                else
                {
                    var _ = collection.FindSync("{ x : 2 }");
                }

                CommandStartedEvent sentCommand = ((CommandStartedEvent)eventCapturer.Events[0]);
                var serverVersion = client
                    .Cluster
                    .Description
                    .Servers
                    .First(s => s.State == ServerState.Connected) // some of nodes may not be initialized yet. So, we need to take only the connected one
                    .Version;
                var clusterType = client.Cluster.Description.Type;

                var expectedContainsReadPreference = clusterType == ClusterType.Standalone ||
                    (clusterType == ClusterType.ReplicaSet && serverVersion < Feature.CommandMessage.FirstSupportedVersion)
                    ? false
                    : true;
                var readPreferenceFieldName = sentCommand.Command.Contains("$readPreference")
                    ? "$readPreference"
                    : "readPreference";

                sentCommand.Command.Contains(readPreferenceFieldName).Should().Be(expectedContainsReadPreference);
            }
        }

        // private methods
        private DisposableMongoClient CreateDisposableClient(EventCapturer eventCapturer, ReadPreference readPreference)
        {
            return DriverTestConfiguration.CreateDisposableClient((MongoClientSettings settings) =>
            {
                settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                settings.ReadPreference = readPreference;
            });
        }
    }
}
