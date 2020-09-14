/* Copyright 2013-present MongoDB Inc.
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

#pragma warning disable CS0618 // Type or member is obsolete
            subject.ConnectionMode.Should().Be(ClusterConnectionMode.Automatic);
            subject.ConnectionModeSwitch.Should().Be(ConnectionModeSwitch.NotSet);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.DirectConnection.Should().Be(null);
            subject.EndPoints.Should().EqualUsing(new[] { new DnsEndPoint("localhost", 27017) }, EndPointHelper.EndPointEqualityComparer);
            subject.LocalThreshold.Should().Be(TimeSpan.FromMilliseconds(15));
            subject.MaxServerSelectionWaitQueueSize.Should().Be(500);
            subject.ReplicaSetName.Should().Be(null);
            subject.Scheme.Should().Be(ConnectionStringScheme.MongoDB);
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
        public void constructor_should_throw_when_localThreshold_is_negative()
        {
            var exception = Record.Exception(() => new ClusterSettings(localThreshold: TimeSpan.FromSeconds(-1)));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("localThreshold");
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
#pragma warning disable CS0618 // Type or member is obsolete
            var connectionMode = ClusterConnectionMode.ReplicaSet;
            var subject = new ClusterSettings(connectionModeSwitch: ConnectionModeSwitch.UseConnectionMode, connectionMode: connectionMode);
            subject.ConnectionMode.Should().Be(connectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_directConnection_should_initialize_instance()
        {
            var directConnection = false;

#pragma warning disable CS0618 // Type or member is obsolete
            var subject = new ClusterSettings(connectionModeSwitch: ConnectionModeSwitch.UseDirectConnection, directConnection: directConnection);

            subject.DirectConnection.Should().Be(directConnection);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_endPoints_should_initialize_instance()
        {
            var endPoints = new[] { new DnsEndPoint("remotehost", 27123) };

            var subject = new ClusterSettings(endPoints: endPoints);

#pragma warning disable CS0618 // Type or member is obsolete
            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.EndPoints.Should().EqualUsing(endPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_localThreshold_should_initialize_instance()
        {
            var localThreshold = TimeSpan.FromSeconds(1);
            var subject = new ClusterSettings(localThreshold: localThreshold);

#pragma warning disable CS0618 // Type or member is obsolete
            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LocalThreshold.Should().Be(localThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_maxServerSelectionWaitQueueSize_should_initialize_instance()
        {
            var maxServerSelectionWaitQueueSize = 123;

            var subject = new ClusterSettings(maxServerSelectionWaitQueueSize: maxServerSelectionWaitQueueSize);

#pragma warning disable CS0618 // Type or member is obsolete
            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(maxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_replicaSetName_should_initialize_instance()
        {
            var replicaSetName = "abc";

            var subject = new ClusterSettings(replicaSetName: replicaSetName);

#pragma warning disable CS0618 // Type or member is obsolete
            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(replicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_scheme_should_initialize_instance()
        {
            var scheme = ConnectionStringScheme.MongoDBPlusSrv;

            var subject = new ClusterSettings(scheme: scheme);

#pragma warning disable CS0618 // Type or member is obsolete
            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_serverSelectionTimeout_should_initialize_instance()
        {
            var serverSelectionTimeout = TimeSpan.FromSeconds(123);

            var subject = new ClusterSettings(serverSelectionTimeout: serverSelectionTimeout);

#pragma warning disable CS0618 // Type or member is obsolete
            subject.ConnectionMode.Should().Be(__defaults.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(serverSelectionTimeout);
        }

        [Theory]
#pragma warning disable CS0618 // Type or member is obsolete
        [InlineData(ConnectionModeSwitch.NotSet, "directConnection", false)]
        [InlineData(ConnectionModeSwitch.NotSet, "connect", false)]
        [InlineData(ConnectionModeSwitch.UseConnectionMode, "directConnection", true)]
        [InlineData(ConnectionModeSwitch.UseConnectionMode, "connect", false)]
        [InlineData(ConnectionModeSwitch.UseDirectConnection, "directConnection", false)]
        [InlineData(ConnectionModeSwitch.UseDirectConnection, "connect", true)]
        public void Property_getter_shoud_throw_when_connectionModeSwitch_is_unexpected(ConnectionModeSwitch connectionModeSwitch, string property, bool shouldFail)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var subject = new ClusterSettings(connectionModeSwitch: connectionModeSwitch);

            Exception exception;
            switch (property)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                case "connect": exception = Record.Exception(() => subject.ConnectionMode); break;
#pragma warning restore CS0618 // Type or member is obsolete
                case "directConnection": exception = Record.Exception(() => subject.DirectConnection); break;
                default: throw new Exception($"Unexpected property {property}.");
            }

            if (shouldFail)
            {
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Fact]
        public void With_connectionMode_should_return_expected_result()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var oldConnectionMode = ClusterConnectionMode.Automatic;
            var newConnectionMode = ClusterConnectionMode.ReplicaSet;
            var subject = new ClusterSettings(connectionModeSwitch: ConnectionModeSwitch.UseConnectionMode, connectionMode: oldConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete

            var result = subject.With(connectionMode: newConnectionMode);

#pragma warning disable CS0618 // Type or member is obsolete
            result.ConnectionMode.Should().Be(newConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_endPoints_should_return_expected_result()
        {
            var oldEndPoints = new[] { new DnsEndPoint("remotehost1", 27123) };
            var newEndPoints = new[] { new DnsEndPoint("remotehost2", 27123) };
            var subject = new ClusterSettings(endPoints: oldEndPoints);

            var result = subject.With(endPoints: newEndPoints);

#pragma warning disable CS0618 // Type or member is obsolete
            result.ConnectionMode.Should().Be(subject.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            result.EndPoints.Should().EqualUsing(newEndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_localThreshold_should_return_expected_result()
        {
            var oldLocalThreshold = TimeSpan.FromSeconds(2);
            var newLocalThreshold = TimeSpan.FromSeconds(1);
            var subject = new ClusterSettings(localThreshold: oldLocalThreshold);

            var result = subject.With(localThreshold: newLocalThreshold);

#pragma warning disable CS0618 // Type or member is obsolete
            result.ConnectionMode.Should().Be(subject.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LocalThreshold.Should().Be(newLocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_maxServerSelectionWaitQueueSize_should_return_expected_result()
        {
            var oldMaxServerSelectionWaitQueueSize = 1;
            var newMaxServerSelectionWaitQueueSize = 2;
            var subject = new ClusterSettings(maxServerSelectionWaitQueueSize: oldMaxServerSelectionWaitQueueSize);

            var result = subject.With(maxServerSelectionWaitQueueSize: newMaxServerSelectionWaitQueueSize);

#pragma warning disable CS0618 // Type or member is obsolete
            result.ConnectionMode.Should().Be(subject.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(newMaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_replicaSetName_should_return_expected_result()
        {
            var oldReplicaSetName = "abc";
            var newReplicaSetName = "def";
            var subject = new ClusterSettings(replicaSetName: oldReplicaSetName);

            var result = subject.With(replicaSetName: newReplicaSetName);

#pragma warning disable CS0618 // Type or member is obsolete
            result.ConnectionMode.Should().Be(subject.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(newReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_scheme_should_return_expected_result()
        {
            var oldScheme = ConnectionStringScheme.MongoDB;
            var newScheme = ConnectionStringScheme.MongoDBPlusSrv;
            var subject = new ClusterSettings(scheme: oldScheme);

            var result = subject.With(scheme: newScheme);

#pragma warning disable CS0618 // Type or member is obsolete
            result.ConnectionMode.Should().Be(subject.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(newScheme);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_serverSelectionTimeout_should_return_expected_result()
        {
            var oldServerSelectionTimeout = TimeSpan.FromSeconds(1);
            var newServerSelectionTimeout = TimeSpan.FromSeconds(2);
            var subject = new ClusterSettings(serverSelectionTimeout: oldServerSelectionTimeout);

            var result = subject.With(serverSelectionTimeout: newServerSelectionTimeout);

#pragma warning disable CS0618 // Type or member is obsolete
            result.ConnectionMode.Should().Be(subject.ConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.ServerSelectionTimeout.Should().Be(newServerSelectionTimeout);
        }
    }
}
