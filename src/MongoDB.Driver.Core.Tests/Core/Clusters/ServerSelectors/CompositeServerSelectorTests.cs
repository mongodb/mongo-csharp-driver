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

using System.Net;
using MongoDB.Driver.Core.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    [TestFixture]
    public class CompositeServerSelectorTests
    {
        private ClusterDescription _description;

        [SetUp]
        public void Setup()
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

        [Test]
        public void Should_run_all_the_selectors()
        {
            var selector1 = Substitute.For<IServerSelector>();
            var selector2 = Substitute.For<IServerSelector>();

            var subject = new CompositeServerSelector(new[] { selector1, selector2 });

            subject.SelectServers(_description, _description.Servers);

            selector1.ReceivedWithAnyArgs().SelectServers(_description, _description.Servers);
            selector2.ReceivedWithAnyArgs().SelectServers(_description, _description.Servers);
        }

        [Test]
        public void Should_pass_on_the_filtered_servers_to_subsequent_selectors()
        {
            var selector1Selected = new[] { _description.Servers[1], _description.Servers[2] };
            var selector1 = Substitute.For<IServerSelector>();
            selector1.SelectServers(null, null).ReturnsForAnyArgs(selector1Selected);
            var selector2 = Substitute.For<IServerSelector>();


            var subject = new CompositeServerSelector(new[] { selector1, selector2 });

            subject.SelectServers(_description, _description.Servers);

            selector1.Received().SelectServers(_description, _description.Servers);
            selector2.Received().SelectServers(_description, selector1Selected);
        }
    }
}