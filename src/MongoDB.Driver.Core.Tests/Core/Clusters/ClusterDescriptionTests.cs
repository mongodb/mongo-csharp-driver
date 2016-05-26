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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using Xunit;

namespace MongoDB.Driver.Core.Clusters
{
    public class ClusterDescriptionTests
    {
        #region static
        // static fields
        private static readonly ClusterId __clusterId;
        private static readonly DnsEndPoint __endPoint1;
        private static readonly DnsEndPoint __endPoint2;
        private static readonly ServerDescription __serverDescription1;
        private static readonly ServerDescription __serverDescription2;
        private static readonly ServerId __serverId1;
        private static readonly ServerId __serverId2;

        // static constructor
        static ClusterDescriptionTests()
        {
            __clusterId = new ClusterId();

            __endPoint1 = new DnsEndPoint("localhost", 27017);
            __endPoint2 = new DnsEndPoint("localhost", 27018);
            __serverId1 = new ServerId(__clusterId, __endPoint1);
            __serverId2 = new ServerId(__clusterId, __endPoint2);
            __serverDescription1 = new ServerDescription(__serverId1, __endPoint1);
            __serverDescription2 = new ServerDescription(__serverId2, __endPoint2);
        }
        #endregion

        // static member tests
        [Fact]
        public void CreateInitial_should_return_initial_description()
        {
            var subject = ClusterDescription.CreateInitial(__clusterId, ClusterConnectionMode.Standalone);
            subject.ClusterId.Should().Be(__clusterId);
            subject.Servers.Should().BeEmpty();
            subject.State.Should().Be(ClusterState.Disconnected);
            subject.ConnectionMode.Should().Be(ClusterConnectionMode.Standalone);
            subject.Type.Should().Be(ClusterType.Unknown);
        }

        // instance member tests
        [Fact]
        public void Constructor_should_initialize_instance()
        {
            var subject = new ClusterDescription(
                __clusterId,
                ClusterConnectionMode.ReplicaSet,
                ClusterType.ReplicaSet,
                new[] { __serverDescription1, __serverDescription2 });
            subject.ClusterId.Should().Be(__clusterId);
            subject.Servers.Should().ContainInOrder(new[] { __serverDescription1, __serverDescription2 });
            subject.State.Should().Be(ClusterState.Disconnected);
            subject.Type.Should().Be(ClusterType.ReplicaSet);
        }

        [Fact]
        public void Equals_should_ignore_revision()
        {
            var subject1 = CreateSubject();
            var subject2 = CreateSubject("Revision");
            subject1.Equals(subject2).Should().BeTrue();
            subject1.Equals((object)subject2).Should().BeTrue();
            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
        }

        [Theory]
        [InlineData("ClusterId")]
        [InlineData("ConnectionMode")]
        [InlineData("Servers")]
        [InlineData("Type")]
        public void Equals_should_return_false_if_any_field_is_not_equal(string notEqualField)
        {
            var subject1 = CreateSubject();
            var subject2 = CreateSubject(notEqualField);
            subject1.Equals(subject2).Should().BeFalse();
            subject1.Equals((object)subject2).Should().BeFalse();
            subject1.GetHashCode().Should().NotBe(subject2.GetHashCode());
        }

        [Fact]
        public void Equals_should_return_true_if_all_fields_are_equal()
        {
            var subject1 = CreateSubject();
            var subject2 = CreateSubject();
            subject1.Equals(subject2).Should().BeTrue();
            subject1.Equals((object)subject2).Should().BeTrue();
            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
        }

        [Fact]
        public void State_should_be_connected_if_any_server_is_connected()
        {
            var connected = ServerDescriptionHelper.Connected(new ClusterId(1));
            var subject = new ClusterDescription(new ClusterId(1), ClusterConnectionMode.Standalone, ClusterType.Standalone, new[] { __serverDescription1, connected });

            subject.State.Should().Be(ClusterState.Connected);
        }

        [Fact]
        public void ToString_should_return_string_representation()
        {
            var subject = new ClusterDescription(new ClusterId(1), ClusterConnectionMode.Standalone, ClusterType.Standalone, new[] { __serverDescription1 });
            var expected = string.Format("{{ ClusterId : \"1\", ConnectionMode : \"Standalone\", Type : \"Standalone\", State : \"Disconnected\", Servers : [{0}] }}",
                __serverDescription1.ToString());
            subject.ToString().Should().Be(expected);
        }

        [Fact]
        public void WithServerDescription_should_add_server_if_server_does_not_exist()
        {
            var subject1 = CreateSubject();
            var newServerDescription = new ServerDescription(new ServerId(__clusterId, new DnsEndPoint("127.0.0.1", 27018)), new DnsEndPoint("127.0.0.1", 27018));
            var subject2 = subject1.WithServerDescription(newServerDescription);
            subject2.Should().NotBeSameAs(subject1);
            subject2.Should().NotBe(subject1);
            subject2.Servers.Count.Should().Be(3);
        }

        [Fact]
        public void WithServerDescription_should_return_new_instance_if_value_is_not_equal()
        {
            var subject1 = CreateSubject();
            var oldServerDescription = subject1.Servers[0];
            var newServerDescription = oldServerDescription.With(
                averageRoundTripTime: oldServerDescription.AverageRoundTripTime.Add(TimeSpan.FromSeconds(1)));
            var subject2 = subject1.WithServerDescription(newServerDescription);
            subject2.Should().NotBeSameAs(subject1);
            subject2.Should().NotBe(subject1);
        }

        [Fact]
        public void WithServerDescription_should_return_same_instance_if_value_is_equal()
        {
            var subject1 = CreateSubject();
            var subject2 = subject1.WithServerDescription(subject1.Servers[0]);
            subject2.Should().BeSameAs(subject1);
        }

        [Fact]
        public void WithoutServerDescription_should_remove_server_if_it_exists()
        {
            var subject1 = CreateSubject();
            var subject2 = subject1.WithoutServerDescription(__endPoint1);
            subject2.Should().NotBeSameAs(subject1);
            subject2.Servers.Count.Should().Be(1);
        }

        [Fact]
        public void WithType_should_return_new_instance_if_value_is_not_equal()
        {
            var subject1 = CreateSubject();
            var subject2 = subject1.WithType(ClusterType.Unknown);
            subject2.Should().NotBeSameAs(subject1);
            subject2.Should().NotBe(subject1);
        }

        [Fact]
        public void WithType_should_return_same_instance_if_value_is_equal()
        {
            var subject1 = CreateSubject();
            var subject2 = subject1.WithType(subject1.Type);
            subject2.Should().BeSameAs(subject1);
        }

        private ClusterDescription CreateSubject(string notEqualField = null)
        {
            var clusterId = new ClusterId(1);
            var connectionMode = ClusterConnectionMode.ReplicaSet;
            var type = ClusterType.ReplicaSet;
            var servers = new[] { __serverDescription1, __serverDescription2 };

            switch (notEqualField)
            {
                case "ClusterId": clusterId = new ClusterId(2); break;
                case "ConnectionMode": connectionMode = ClusterConnectionMode.Standalone; break;
                case "Type": type = ClusterType.Unknown; break;
                case "Servers": servers = new[] { __serverDescription1 }; break;
            }

            return new ClusterDescription(clusterId, connectionMode, type, servers);
        }
    }
}
