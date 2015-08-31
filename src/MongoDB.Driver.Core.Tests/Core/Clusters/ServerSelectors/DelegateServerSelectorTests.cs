﻿/* Copyright 2013-2015 MongoDB Inc.
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
using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    [TestFixture]
    public class DelegateServerSelectorTests
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
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27017), averageRoundTripTime: TimeSpan.FromMilliseconds(10)),
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018), averageRoundTripTime: TimeSpan.FromMilliseconds(30)),
                    ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27019), averageRoundTripTime: TimeSpan.FromMilliseconds(20))
                });
        }

        [Test]
        public void Should_select_the_servers_matching_the_delegate()
        {
            var subject = new DelegateServerSelector((c, s) => s.Where(p => ((DnsEndPoint)p.EndPoint).Port >= 27018));

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Count.Should().Be(2);
            result.Should().BeEquivalentTo(new[] { _description.Servers[1], _description.Servers[2] });
        }

        [Test]
        public void Should_select_no_servers_when_none_match()
        {
            var subject = new DelegateServerSelector((c, s) => s.Where(p => ((DnsEndPoint)p.EndPoint).Port == 0));

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEmpty();
        }

        [Test]
        public void Should_select_no_servers_when_none_exist()
        {
            var subject = new DelegateServerSelector((c, s) => s.Where(p => ((DnsEndPoint)p.EndPoint).Port == 0));

            var result = subject.SelectServers(_description, Enumerable.Empty<ServerDescription>()).ToList();

            result.Should().BeEmpty();
        }
    }
}