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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.EqualityComparers;
using MongoDB.Driver.Core.Misc;
using MongoDB.TestHelpers.XunitExtensions;
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

            subject.CryptClientSettings.Should().Be(null);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(new[] { new DnsEndPoint("localhost", 27017) }, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
            subject.LocalThreshold.Should().Be(TimeSpan.FromMilliseconds(15));
            subject.MaxServerSelectionWaitQueueSize.Should().Be(500);
            subject.ReplicaSetName.Should().Be(null);
            subject.Scheme.Should().Be(ConnectionStringScheme.MongoDB);
            subject.ServerApi.Should().BeNull();
            subject.ServerSelectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
            subject.SrvMaxHosts.Should().Be(0);
            subject.SrvServiceName.Should().Be("mongodb");
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
        public void constructor_with_directConnection_should_initialize_instance()
        {
            const bool directConnection = true;

            var subject = new ClusterSettings(directConnection: directConnection);

            subject.DirectConnection.Should().Be(directConnection);
            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
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

            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(endPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_locadBalanced_should_initialize_instance()
        {
            var loadBalanced = true;
            var subject = new ClusterSettings(loadBalanced: loadBalanced);

            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().Be(loadBalanced);
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_infinite_localThreshold_should_initialize_instance()
        {
            var subject = new ClusterSettings(localThreshold: Timeout.InfiniteTimeSpan);
            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
            subject.LocalThreshold.Should().Be(Timeout.InfiniteTimeSpan);
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

            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
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

            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
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

            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
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

            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(scheme);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_serverApi_should_initialize_instance()
        {
            var serverApi = new ServerApi(ServerApiVersion.V1, true, true);

            var subject = new ClusterSettings(serverApi: serverApi);

            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerApi.Should().Be(serverApi);
            subject.ServerSelectionTimeout.Should().Be(__defaults.ServerSelectionTimeout);
        }

        [Fact]
        public void constructor_with_serverSelectionTimeout_should_initialize_instance()
        {
            var serverSelectionTimeout = TimeSpan.FromSeconds(123);

            var subject = new ClusterSettings(serverSelectionTimeout: serverSelectionTimeout);

            subject.CryptClientSettings.Should().Be(__defaults.CryptClientSettings);
            subject.DirectConnection.Should().Be(false);
            subject.EndPoints.Should().EqualUsing(__defaults.EndPoints, EndPointHelper.EndPointEqualityComparer);
            subject.LoadBalanced.Should().BeFalse();
            subject.LocalThreshold.Should().Be(__defaults.LocalThreshold);
            subject.MaxServerSelectionWaitQueueSize.Should().Be(__defaults.MaxServerSelectionWaitQueueSize);
            subject.ReplicaSetName.Should().Be(__defaults.ReplicaSetName);
            subject.Scheme.Should().Be(__defaults.Scheme);
            subject.ServerSelectionTimeout.Should().Be(serverSelectionTimeout);
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_with_valid_srvMaxHosts_should_initialize_instance([Values(0,42)]int srvMaxHosts)
        {
            var subject = new ClusterSettings(srvMaxHosts: srvMaxHosts);

            subject.SrvMaxHosts.Should().Be(srvMaxHosts);
        }

        [Fact]
        public void Constructor_with_negative_srvMaxHosts_should_throw()
        {
            var exception = Record.Exception(() => new ClusterSettings(srvMaxHosts: -1));

            exception.Should().BeOfType<ArgumentOutOfRangeException>()
                .Subject.ParamName.Should().Be("srvMaxHosts");
        }

        [Fact]
        public void Constructor_with_srvServiceName_should_initialize_instance()
        {
            var srvServiceName = "customname";
            var subject = new ClusterSettings(srvServiceName: srvServiceName);

            subject.SrvServiceName.Should().Be(srvServiceName);
        }

        [Fact]
        public void With_cryptClientSettings_should_return_expected_result()
        {
            var newCryptClientSettings = new CryptClientSettings(
               true,
               "csfleLibPath",
               "csfleSearchPath",
               null,
               true,
               null,
               null);

            var subject = new ClusterSettings();
            var result = subject.With(cryptClientSettings: newCryptClientSettings);

            result.CryptClientSettings.Should().Be(newCryptClientSettings);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LoadBalanced.Should().BeFalse();
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

            result.CryptClientSettings.Should().Be(subject.CryptClientSettings);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.EndPoints.Should().EqualUsing(newEndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LoadBalanced.Should().BeFalse();
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_loadBalanced_should_return_expected_result()
        {
            var oldLoadBalanced = false;
            var newLoadBalanced = true;
            var subject = new ClusterSettings(loadBalanced: oldLoadBalanced);

            var result = subject.With(loadBalanced: newLoadBalanced);

            result.CryptClientSettings.Should().Be(subject.CryptClientSettings);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LoadBalanced.Should().Be(newLoadBalanced);
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

            result.CryptClientSettings.Should().Be(subject.CryptClientSettings);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LoadBalanced.Should().BeFalse();
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

            result.CryptClientSettings.Should().Be(subject.CryptClientSettings);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LoadBalanced.Should().BeFalse();
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

            result.CryptClientSettings.Should().Be(subject.CryptClientSettings);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LoadBalanced.Should().BeFalse();
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

            result.CryptClientSettings.Should().Be(subject.CryptClientSettings);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LoadBalanced.Should().BeFalse();
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(newScheme);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_serverApi_should_return_expected_result()
        {
            var oldServerApi = new ServerApi(ServerApiVersion.V1, true, true);
            var newServerApi = new ServerApi(ServerApiVersion.V1);
            var subject = new ClusterSettings(serverApi: oldServerApi);

            var result = subject.With(serverApi: newServerApi);

            result.CryptClientSettings.Should().Be(subject.CryptClientSettings);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LoadBalanced.Should().BeFalse();
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.ServerApi.Should().Be(newServerApi);
            result.ServerSelectionTimeout.Should().Be(subject.ServerSelectionTimeout);
        }

        [Fact]
        public void With_serverSelectionTimeout_should_return_expected_result()
        {
            var oldServerSelectionTimeout = TimeSpan.FromSeconds(1);
            var newServerSelectionTimeout = TimeSpan.FromSeconds(2);
            var subject = new ClusterSettings(serverSelectionTimeout: oldServerSelectionTimeout);

            var result = subject.With(serverSelectionTimeout: newServerSelectionTimeout);

            result.CryptClientSettings.Should().Be(subject.CryptClientSettings);
            result.DirectConnection.Should().Be(subject.DirectConnection);
            result.EndPoints.Should().EqualUsing(subject.EndPoints, EndPointHelper.EndPointEqualityComparer);
            result.LoadBalanced.Should().BeFalse();
            result.LocalThreshold.Should().Be(subject.LocalThreshold);
            result.MaxServerSelectionWaitQueueSize.Should().Be(subject.MaxServerSelectionWaitQueueSize);
            result.ReplicaSetName.Should().Be(subject.ReplicaSetName);
            result.Scheme.Should().Be(subject.Scheme);
            result.ServerSelectionTimeout.Should().Be(newServerSelectionTimeout);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_valid_srvMaxHosts_should_return_expected_result([Values(0, 42)]int srvMaxHosts)
        {
            var subject = new ClusterSettings(srvMaxHosts: 5);

            var result = subject.With(srvMaxHosts: srvMaxHosts);

            result.SrvMaxHosts.Should().Be(srvMaxHosts);
        }

        [Fact]
        public void With_srvServiceName_should_return_expected_result()
        {
            var srvServiceName = "customname";
            var subject = new ClusterSettings();

            var result = subject.With(srvServiceName: srvServiceName);

            result.SrvServiceName.Should().Be(srvServiceName);
        }

        [Fact]
        public void With_negative_srvMaxHosts_should_throw()
        {
            var subject = new ClusterSettings();

            var exception = Record.Exception(() => subject.With(srvMaxHosts: -1));

            exception.Should().BeOfType<ArgumentOutOfRangeException>()
                .Subject.ParamName.Should().Be("srvMaxHosts");
        }
    }
}
