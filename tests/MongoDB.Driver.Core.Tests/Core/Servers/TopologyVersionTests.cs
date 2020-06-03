/* Copyright 2020⁠–⁠present MongoDB Inc.
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
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class TopologyVersionTests
    {
        [Fact]
        public void Constructor_should_properly_initialize_instance()
        {
            var processId = ObjectId.Empty;
            var counter = 42L;

            var subject = new TopologyVersion(processId, counter);

            subject.ProcessId.Should().Be(processId);
            subject.Counter.Should().Be(counter);
        }

        [Fact]
        public void CompareTopologyVersion_should_always_return_negative_if_any_comparand_is_null()
        {
            var processId = ObjectId.Empty;
            var counter = 31L;

            TopologyVersion nullTopologyVersion = null;
            TopologyVersion nonNullTopologyVersion = new TopologyVersion(processId, counter);

            TopologyVersion.CompareTopologyVersion(nullTopologyVersion, nonNullTopologyVersion).Should().BeNegative();
            TopologyVersion.IsStalerThan(nullTopologyVersion, nonNullTopologyVersion).Should().BeTrue();
            TopologyVersion.IsFresherThan(nullTopologyVersion, nonNullTopologyVersion).Should().BeFalse();

            TopologyVersion.CompareTopologyVersion(nonNullTopologyVersion, nullTopologyVersion).Should().BeNegative();
            TopologyVersion.IsStalerThan(nonNullTopologyVersion, nullTopologyVersion).Should().BeTrue();
            TopologyVersion.IsFresherThan(nonNullTopologyVersion, nullTopologyVersion).Should().BeFalse();

            TopologyVersion.CompareTopologyVersion(nullTopologyVersion, nullTopologyVersion).Should().BeNegative();
            TopologyVersion.IsStalerThan(nullTopologyVersion, nullTopologyVersion).Should().BeTrue();
            TopologyVersion.IsFresherThan(nullTopologyVersion, nullTopologyVersion).Should().BeFalse();
        }

        [Fact]
        public void CompareTopologyVersion_should_always_return_negative_if_processIds_are_not_equal()
        {
            var processId1 = ObjectId.Empty;
            var processId2 = ObjectId.GenerateNewId();
            var counter = 42L;

            var subject1 = new TopologyVersion(processId1, counter);
            var subject2 = new TopologyVersion(processId2, counter);
            var local = subject1;
            var serverResponse = subject2;

            TopologyVersion.CompareTopologyVersion(local, serverResponse).Should().BeNegative();
            TopologyVersion.IsStalerThan(local, serverResponse).Should().BeTrue();
            TopologyVersion.IsFresherThan(local, serverResponse).Should().BeFalse();

            // changing the order should change the results
            local = subject2;
            serverResponse = subject1;

            TopologyVersion.CompareTopologyVersion(local, serverResponse).Should().BeNegative();
            TopologyVersion.IsStalerThan(local, serverResponse).Should().BeTrue();
            TopologyVersion.IsFresherThan(local, serverResponse).Should().BeFalse();
        }

        [Fact]
        public void CompareTopologyVersion_should_return_expected_when_processIds_are_equal_and_one_has_bigger_counter()
        {
            var processId1 = ObjectId.Empty;
            var processId2 = ObjectId.Empty;
            var counter = 42L;

            var older = new TopologyVersion(processId1, counter);
            var newer = new TopologyVersion(processId2, counter + 1);

            TopologyVersion.CompareTopologyVersion(older, newer).Should().BeNegative();
            TopologyVersion.IsStalerThan(older, newer).Should().BeTrue();
            TopologyVersion.IsFresherThan(older, newer).Should().BeFalse();

            TopologyVersion.CompareTopologyVersion(newer, older).Should().BePositive();
            TopologyVersion.IsStalerThan(newer, older).Should().BeFalse();
            TopologyVersion.IsFresherThan(newer, older).Should().BeTrue();
        }

        [Fact]
        public void CompareTopologyVersion_should_return_zero_when_topologies_are_equal()
        {
            var processId = ObjectId.Empty;
            var counter = 31L;

            var local = new TopologyVersion(processId, counter);
            var serverResponse = local;

            TopologyVersion.CompareTopologyVersion(local, serverResponse).Should().Be(0);

            TopologyVersion.IsFresherThanOrEqualTo(local, serverResponse).Should().BeTrue();
            TopologyVersion.IsFresherThan(local, serverResponse).Should().BeFalse();

            TopologyVersion.IsStalerThanOrEqualTo(local, serverResponse).Should().BeTrue();
            TopologyVersion.IsStalerThan(local, serverResponse).Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_if_processIds_are_equal_and_counters_are_not_equal()
        {
            var processId = ObjectId.Empty;
            var counter = 42L;

            var subject1 = new TopologyVersion(processId, counter);
            var subject2 = new TopologyVersion(processId, counter + 1);

            subject1.Equals(subject2).Should().BeFalse();
            subject1.Equals((object)subject2).Should().BeFalse();
            subject1.GetHashCode().Should().NotBe(subject2.GetHashCode());

            subject2.Equals(subject1).Should().BeFalse();
            subject2.Equals((object)subject1).Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_if_processId_is_not_equal()
        {
            var processId1 = ObjectId.Empty;
            var processId2 = ObjectId.GenerateNewId();
            var counter = 42L;

            var subject1 = new TopologyVersion(processId1, counter);
            var subject2 = new TopologyVersion(processId2, counter);

            subject1.Equals(subject2).Should().BeFalse();
            subject1.Equals((object)subject2).Should().BeFalse();
            subject1.GetHashCode().Should().NotBe(subject2.GetHashCode());

            subject2.Equals(subject1).Should().BeFalse();
            subject2.Equals((object)subject1).Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_true_if_all_fields_are_equal()
        {
            var processId = ObjectId.Empty;
            var counter = 42L;

            var subject1 = new TopologyVersion(processId, counter);
            var subject2 = new TopologyVersion(processId, counter);

            subject1.Equals(subject2).Should().BeTrue();
            subject2.Equals(subject1).Should().BeTrue();

            subject1.Equals((object)subject2).Should().BeTrue();
            subject2.Equals((object)subject1).Should().BeTrue();

            subject1.GetHashCode().Should().Be(subject2.GetHashCode());
            subject2.GetHashCode().Should().Be(subject1.GetHashCode());
        }

        [Fact]
        public void FromBsonDocument_should_return_TopologyDescription_when_supplied_valid_BsonDocument()
        {
            var processId = ObjectId.Empty;
            var counter = 31L;
            var topologyVersionDocument = new BsonDocument {{"processId", processId}, {"counter", counter}};

            TopologyVersion subject = TopologyVersion.FromBsonDocument(topologyVersionDocument);

            subject.Should().NotBeNull();
        }

        [Fact]
        public void FromBsonDocument_should_return_null_when_supplied_invalid_BsonDocument()
        {
            var invalidTopologyVersionDocument = new BsonDocument("counter", 31);

            TopologyVersion subject = TopologyVersion.FromBsonDocument(invalidTopologyVersionDocument);

            subject.Should().BeNull();
        }

        [Fact]
        public void ToBsonDocument_should_return_expected()
        {
            var processId = ObjectId.Empty;
            var counter = 31L;

            var subject = new TopologyVersion(processId, counter);
            var document = subject.ToBsonDocument();

            document["counter"].Should().Be(counter);
            document["processId"].Should().Be(processId);
        }
    }
}
