/* Copyright 2020-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class CustomServerSelectorTests
    {
        [Fact]
        public void Should_call_custom_server_selector()
        {
            var eventCapturer = new EventCapturer()
                .Capture<ClusterSelectingServerEvent>()
                .Capture<ClusterSelectedServerEvent>();
            var customServerSelector = new CustomServerSelector();
            using (var client = DriverTestConfiguration.CreateDisposableClient(
                clientSettings =>
                    clientSettings.ClusterConfigurator =
                        c =>
                        {
                            c.ConfigureCluster(
                                s =>
                                    new ClusterSettings(
                                        postServerSelector: customServerSelector));
                            c.Subscribe(eventCapturer);
                        }))
            {
                var collection = client
                    .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .WithReadPreference(ReadPreference.Nearest);

                customServerSelector.CustomSelectorWasCalled = false;
                eventCapturer.Clear();

                collection.CountDocuments(new BsonDocument());

                customServerSelector.CustomSelectorWasCalled.Should().BeTrue();
                eventCapturer.Next().Should().BeOfType<ClusterSelectingServerEvent>();
                eventCapturer.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            }
        }

        // nested types
        private class CustomServerSelector : IServerSelector
        {
            public bool CustomSelectorWasCalled { get; set; }

            public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
            {
                CustomSelectorWasCalled = true;

                return servers;
            }
        }
    }
}
