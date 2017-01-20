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
using System.Net;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.EqualityComparers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Configuration
{
    public class ClusterSettingsTests
    {
        private static readonly ClusterSettings __defaults = new ClusterSettings();

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var subject = new ClusterSettings();

            subject.ConnectionMode.Should().Be(ClusterConnectionMode.Automatic);
            subject.EndPoints.Should().EqualUsing(new[] { new DnsEndPoint("localhost", 27017) }, EndPointHelper.EndPointEqualityComparer);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(500);
            subject.ReplicaSetName.Should().Be(null);
            subject.ServerSelectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void constructor_should_throw_when_endPoints_is_null()
        {
            Action action = () => new ClusterSettings(endPoints: null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("endPoints");
        }

        [Fact]
        public void constructor_should_throw_when_serverSelectionTimeout_is_negative()
        {
            Action action = () => new ClusterSettings(serverSelectionTimeout: TimeSpan.FromSeconds(-1));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("serverSelectionTimeout");
        }

        [Fact]
        public void constructor_should_throw_when_maxServerSelectionWaitQueueSize_is_negative()
        {
            Action action = () => new ClusterSettings(maxServerSelectionWaitQueueSize: -1);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("maxServerSelectionWaitQueueSize");
        }

        [Fact]
        public void constructor_with_connectionMode_should_initialize_instance()
        {
            var connectionMode = ClusterConnectionMode.ReplicaSet;

            var subject = new ClusterSettings(connectionMode: connectionMode);

            subject.ConnectionMode.Should().Be(connectionMode);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_endPoints_should_initialize_instance()
        {
            var endPoints = new[] { new DnsEndPoint("remotehost", 27123) };

            var subject = new ClusterSettings(endPoints: endPoints);

            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
            subject.EndPoints.Should().EqualUsing(endPoints, EndPointHelper.EndPointEqualityComparer);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_maxServerSelectionWaitQueueSize_should_initialize_instance()
        {
            var maxServerSelectionWaitQueueSize = 123;

            var subject = new ClusterSettings(maxServerSelectionWaitQueueSize: maxServerSelectionWaitQueueSize);

            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(maxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_replicaSetName_should_initialize_instance()
        {
            var replicaSetName = "abc";

            var subject = new ClusterSettings(replicaSetName: replicaSetName);

            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(replicaSetName);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_serverSelectionTimeout_should_initialize_instance()
        {
            var serverSelectionTimeout = TimeSpan.FromSeconds(123);

            var subject = new ClusterSettings(serverSelectionTimeout: serverSelectionTimeout);

            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.ServerSelectionTimeout.Should().Be(serverSelectionTimeout);
        }

        [Fact]
        public void With_connectionMode_should_return_expected_result()
        {
            var oldConnectionMode = ClusterConnectionMode.Automatic;
            var newConnectionMode = ClusterConnectionMode.ReplicaSet;
            var subject = new ClusterSettings(connectionMode: oldConnectionMode);

            var result = subject.With(connectionMode: newConnectionMode);

            result.ConnectionMode.Should().Be(newConnectionMode);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_endPoints_should_return_expected_result()
        {
            var oldEndPoints = new[] { new DnsEndPoint("remotehost1", 27123) };
            var newEndPoints = new[] { new DnsEndPoint("remotehost2", 27123) };
            var subject = new ClusterSettings(endPoints: oldEndPoints);

            var result = subject.With(endPoints: newEndPoints);

            result.ConnectionMode.Should().Be(subject.ConnectionMode);
            result.EndPoints.Should().EqualUsing(newEndPoints, EndPointHelper.EndPointEqualityComparer);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_maxServerSelectionWaitQueueSize_should_return_expected_result()
        {
            var oldMaxServerSelectionWaitQueueSize = 1;
            var newMaxServerSelectionWaitQueueSize = 2;
            var subject = new ClusterSettings(maxServerSelectionWaitQueueSize: oldMaxServerSelectionWaitQueueSize);

            var result = subject.With(maxServerSelectionWaitQueueSize: newMaxServerSelectionWaitQueueSize);

            result.ConnectionMode.Should().Be(subject.ConnectionMode);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.MaxServerSelectionWaitQueueSize.Should().Be(newMaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_replicaSetName_should_return_expected_result()
        {
            var oldReplicaSetName = "abc";
            var newReplicaSetName = "def";
            var subject = new ClusterSettings(replicaSetName: oldReplicaSetName);

            var result = subject.With(replicaSetName: newReplicaSetName);

            result.ConnectionMode.Should().Be(subject.ConnectionMode);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(newReplicaSetName);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_serverSelectionTimeout_should_return_expected_result()
        {
            var oldServerSelectionTimeout = TimeSpan.FromSeconds(1);
            var newServerSelectionTimeout = TimeSpan.FromSeconds(2);
            var subject = new ClusterSettings(serverSelectionTimeout: oldServerSelectionTimeout);

            var result = subject.With(serverSelectionTimeout: newServerSelectionTimeout);

            result.ConnectionMode.Should().Be(subject.ConnectionMode);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.ServerSelectionTimeout.Should().Be(newServerSelectionTimeout);
        }
    }
}
