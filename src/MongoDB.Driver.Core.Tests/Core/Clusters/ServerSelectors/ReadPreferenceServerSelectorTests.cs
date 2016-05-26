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

using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    public class ReadPreferenceServerSelectorTests
    {
        private ClusterDescription _description;
        private ServerDescription _primary;
        private ServerDescription _secondary1;
        private ServerDescription _secondary2;

        public ReadPreferenceServerSelectorTests()
        {
            var clusterId = new ClusterId();
            _primary = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27017), ServerType.ReplicaSetPrimary, new TagSet(new[] { new Tag("a", "1") }));
            _secondary1 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018), ServerType.ReplicaSetSecondary, new TagSet(new[] { new Tag("a", "1") }));
            _secondary2 = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27019), ServerType.ReplicaSetSecondary, new TagSet(new[] { new Tag("a", "2") }));

            _description = new ClusterDescription(
                clusterId,
                ClusterConnectionMode.ReplicaSet,
                ClusterType.ReplicaSet,
                new[] { _primary, _secondary1, _secondary2 });
        }

        [Fact]
        public void Primary_should_select_the_primary()
        {
            var subject = new ReadPreferenceServerSelector(ReadPreference.Primary);

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { _primary });
        }

        [Fact]
        public void PrimaryPreferred_should_select_the_primary_regardless_of_tags()
        {
            var subject = new ReadPreferenceServerSelector(new ReadPreference(ReadPreferenceMode.PrimaryPreferred, new[] { new TagSet(new[] { new Tag("a", "2") }) }));

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { _primary });
        }

        [Fact]
        public void Primary_should_select_none_when_no_primary_exists()
        {
            var subject = new ReadPreferenceServerSelector(ReadPreference.Primary);

            var result = subject.SelectServers(_description, new[] { _secondary1, _secondary2 }).ToList();

            result.Should().BeEmpty();
        }

        [Fact]
        public void Secondary_should_select_a_secondary()
        {
            var subject = new ReadPreferenceServerSelector(ReadPreference.Secondary);

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { _secondary1, _secondary2 });
        }

        [Fact]
        public void Secondary_should_select_only_secondaries_when_they_match_the_tags()
        {
            var subject = new ReadPreferenceServerSelector(new ReadPreference(ReadPreferenceMode.Secondary, new[] { new TagSet(new[] { new Tag("a", "1") }) }));

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { _secondary1 });
        }

        [Fact]
        public void Secondary_should_select_none_when_no_secondaries_exist()
        {
            var subject = new ReadPreferenceServerSelector(ReadPreference.Secondary);

            var result = subject.SelectServers(_description, new[] { _primary }).ToList();

            result.Should().BeEmpty();
        }

        [Fact]
        public void SecondaryPreferred_should_select_all_the_secondaries()
        {
            var subject = new ReadPreferenceServerSelector(ReadPreference.SecondaryPreferred);

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { _secondary1, _secondary2 });
        }

        [Fact]
        public void SecondaryPreferred_should_select_the_primary_when_no_secondaries_exist()
        {
            var subject = new ReadPreferenceServerSelector(ReadPreference.SecondaryPreferred);

            var result = subject.SelectServers(_description, new[] { _primary }).ToList();

            result.Should().BeEquivalentTo(new[] { _primary });
        }

        [Fact]
        public void SecondaryPreferred_should_select_secondaries_that_match_the_tags()
        {
            var subject = new ReadPreferenceServerSelector(new ReadPreference(ReadPreferenceMode.SecondaryPreferred, new[] { new TagSet(new[] { new Tag("a", "1") }) }));

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { _secondary1 });
        }

        [Fact]
        public void SecondaryPreferred_should_select_the_primary_when_no_secondaries_exist_regardless_of_tags()
        {
            var subject = new ReadPreferenceServerSelector(new ReadPreference(ReadPreferenceMode.SecondaryPreferred, new[] { new TagSet(new[] { new Tag("a", "2") }) }));

            var result = subject.SelectServers(_description, new[] { _primary, _secondary1 }).ToList();

            result.Should().BeEquivalentTo(new[] { _primary });
        }

        [Fact]
        public void PrimaryPreferred_should_select_the_primary_when_it_exists()
        {
            var subject = new ReadPreferenceServerSelector(ReadPreference.PrimaryPreferred);

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { _primary });
        }

        [Fact]
        public void PrimaryPreferred_should_select_the_primary_when_it_exists_regardless_of_tags()
        {
            var subject = new ReadPreferenceServerSelector(new ReadPreference(ReadPreferenceMode.PrimaryPreferred, new[] { new TagSet(new[] { new Tag("a", "2") }) }));

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { _primary });
        }

        [Fact]
        public void PrimaryPreferred_should_select_the_secondaries_when_no_primary_exists()
        {
            var subject = new ReadPreferenceServerSelector(ReadPreference.PrimaryPreferred);

            var result = subject.SelectServers(_description, new[] { _secondary1, _secondary2 }).ToList();

            result.Should().BeEquivalentTo(new[] { _secondary1, _secondary2 });
        }

        [Fact]
        public void PrimaryPreferred_should_select_the_secondaries_when_no_primary_exists_when_tags_exist()
        {
            var subject = new ReadPreferenceServerSelector(new ReadPreference(ReadPreferenceMode.PrimaryPreferred, new[] { new TagSet(new[] { new Tag("a", "2") }) }));

            var result = subject.SelectServers(_description, new[] { _secondary1, _secondary2 }).ToList();

            result.Should().BeEquivalentTo(new[] { _secondary2 });
        }

        [Fact]
        public void Nearest_should_select_all_the_servers()
        {
            var subject = new ReadPreferenceServerSelector(ReadPreference.Nearest);

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(_description.Servers);
        }

        [Fact]
        public void Nearest_should_select_all_the_servers_respecting_tags()
        {
            var subject = new ReadPreferenceServerSelector(new ReadPreference(ReadPreferenceMode.Nearest, new[] { new TagSet(new[] { new Tag("a", "1") }) }));

            var result = subject.SelectServers(_description, _description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { _primary, _secondary1 });
        }

        [Fact]
        public void Should_select_nothing_when_attempting_to_match_tags_and_servers_do_not_have_tags()
        {
            var clusterId = new ClusterId();
            var primary = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27017), ServerType.ReplicaSetPrimary);
            var secondary = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018), ServerType.ReplicaSetSecondary);

            var description = new ClusterDescription(
                clusterId,
                ClusterConnectionMode.ReplicaSet,
                ClusterType.ReplicaSet,
                new[] { primary, secondary });

            var subject = new ReadPreferenceServerSelector(new ReadPreference(ReadPreferenceMode.Secondary, new[] { new TagSet(new[] { new Tag("a", "1") }) }));

            var result = subject.SelectServers(description, description.Servers).ToList();

            result.Should().BeEmpty();
        }

        [Fact]
        public void ReadPreference_should_be_ignored_when_directly_connected_with_a_server()
        {
            var subject = new ReadPreferenceServerSelector(new ReadPreference(ReadPreferenceMode.Primary));

            var clusterId = new ClusterId();
            var server = ServerDescriptionHelper.Connected(clusterId, new DnsEndPoint("localhost", 27018), ServerType.ReplicaSetSecondary);

            var description = new ClusterDescription(
                clusterId,
                ClusterConnectionMode.Direct,
                ClusterType.ReplicaSet,
                new[] { server });

            var result = subject.SelectServers(description, description.Servers).ToList();

            result.Should().BeEquivalentTo(new[] { server });
        }
    }
}