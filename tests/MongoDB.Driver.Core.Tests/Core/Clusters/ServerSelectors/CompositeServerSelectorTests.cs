/* Copyright 2013-2016 MongoDB Inc.
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
using System.Net;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    public class CompositeServerSelectorTests
    {
        private ClusterDescription _description;

        public CompositeServerSelectorTests()
        {
            var clusterId = new ClusterId();
            _description = new ClusterDescription(
                clusterId,
                ClusterConnectionMode.Automatic,
                ClusterType.Unknown,
                new[]
                {
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27017)),
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018)),
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27019)),
                });
        }

        [Fact]
        public void Should_run_all_the_selectors()
        {
            var mockSelector1 = new Mock<IServerSelector>();
            var mockSelector2 = new Mock<IServerSelector>();
            mockSelector1.Setup(s => s.SelectServers(_description, _description.Servers)).Returns(_description.Servers);

            var subject = new CompositeServerSelector(new[] { mockSelector1.Object, mockSelector2.Object });

            subject.SelectServers(_description, _description.Servers);

            mockSelector1.Verify(s => s.SelectServers(_description, _description.Servers), Times.Once);
            mockSelector2.Verify(s => s.SelectServers(_description, _description.Servers), Times.Once);
        }

        [Fact]
        public void Should_pass_on_the_filtered_servers_to_subsequent_selectors()
        {
            var selector1Selected = new[] { _description.Servers[1], _description.Servers[2] };
            var mockSelector1 = new Mock<IServerSelector>();
            mockSelector1.Setup(s => s.SelectServers(_description, _description.Servers)).Returns(selector1Selected);
            var mockSelector2 = new Mock<IServerSelector>();

            var subject = new CompositeServerSelector(new[] { mockSelector1.Object, mockSelector2.Object });

            subject.SelectServers(_description, _description.Servers);

            mockSelector1.Verify(s => s.SelectServers(_description, _description.Servers), Times.Once);
            mockSelector2.Verify(s => s.SelectServers(_description, selector1Selected), Times.Once);
        }
    }
}