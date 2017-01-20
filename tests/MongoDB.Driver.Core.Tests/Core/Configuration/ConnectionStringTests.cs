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
using System.Net.Sockets;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Operations;
using Xunit;

namespace MongoDB.Driver.Core.Configuration
{
    [Trait("Category", "ConnectionString")]
    public class ConnectionStringTests
    {
        [Fact]
        public void With_one_host_and_no_port()
        {
            var subject = new ConnectionString("mongodb://localhost");

            subject.Hosts.Count().Should().Be(1);
            subject.Hosts.Single().Should().Be(new DnsEndPoint("localhost", 27017));
        }

        [Fact]
        public void With_one_host_and_port()
        {
            var subject = new ConnectionString("mongodb://localhost:27092");

            subject.Hosts.Count().Should().Be(1);
            subject.Hosts.Single().Should().Be(new DnsEndPoint("localhost", 27092));
        }

        [Fact]
        public void With_two_hosts_and_one_port()
        {
            var subject = new ConnectionString("mongodb://localhost:27092,remote");

            subject.Hosts.Count().Should().Be(2);
            subject.Hosts[0].Should().Be(new DnsEndPoint("localhost", 27092));
            subject.Hosts[1].Should().Be(new DnsEndPoint("remote", 27017));
        }

        [Fact]
        public void With_two_hosts_and_one_port2()
        {
            var subject = new ConnectionString("mongodb://localhost,remote:27092");

            subject.Hosts.Count().Should().Be(2);
            subject.Hosts[0].Should().Be(new DnsEndPoint("localhost", 27017));
            subject.Hosts[1].Should().Be(new DnsEndPoint("remote", 27092));
        }

        [Fact]
        public void With_two_hosts_and_two_ports()
        {
            var subject = new ConnectionString("mongodb://localhost:30000,remote:27092");

            subject.Hosts.Count().Should().Be(2);
            subject.Hosts[0].Should().Be(new DnsEndPoint("localhost", 30000));
            subject.Hosts[1].Should().Be(new DnsEndPoint("remote", 27092));
        }

        [Fact]
        public void With_an_ipv4_host()
        {
            var subject = new ConnectionString("mongodb://127.0.0.1");

            subject.Hosts.Count().Should().Be(1);
            subject.Hosts[0].Should().Be(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27017));
        }

        [Fact]
        public void With_an_ipv4_host_and_port()
        {
            var subject = new ConnectionString("mongodb://127.0.0.1:28017");

            subject.Hosts.Count().Should().Be(1);
            subject.Hosts[0].Should().Be(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 28017));
        }

        [Fact]
        public void With_an_ipv6_host()
        {
            var subject = new ConnectionString("mongodb://[::1]");

            subject.Hosts.Count().Should().Be(1);
            subject.Hosts[0].Should().Be(new IPEndPoint(IPAddress.Parse("[::1]"), 27017));
        }

        [Fact]
        public void With_a_2_ipv6_hosts()
        {
            var subject = new ConnectionString("mongodb://[::1],[::2]");

            subject.Hosts.Count().Should().Be(2);
            subject.Hosts[0].Should().Be(new IPEndPoint(IPAddress.Parse("[::1]"), 27017));
            subject.Hosts[1].Should().Be(new IPEndPoint(IPAddress.Parse("[::2]"), 27017));
        }

        [Fact]
        public void With_an_ipv6_host_and_port()
        {
            var subject = new ConnectionString("mongodb://[::1]:28017");

            subject.Hosts.Count().Should().Be(1);
            subject.Hosts[0].Should().Be(new IPEndPoint(IPAddress.Parse("[::1]"), 28017));
        }

        [Fact]
        public void With_three_hosts_of_different_types()
        {
            var subject = new ConnectionString("mongodb://localhost,10.0.0.1:30000,[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]:28017");

            subject.Hosts.Count().Should().Be(3);
            subject.Hosts[0].Should().Be(new DnsEndPoint("localhost", 27017, AddressFamily.Unspecified));
            subject.Hosts[1].Should().Be(new IPEndPoint(IPAddress.Parse("10.0.0.1"), 30000));
            subject.Hosts[2].Should().Be(new IPEndPoint(IPAddress.Parse("[FE80:0000:0000:0000:0202:B3FF:FE1E:8329]"), 28017));
        }

        [Theory]
        [InlineData("mongodb://localhost")]
        [InlineData("mongodb://localhost/")]
        public void When_nothing_is_specified(string connectionString)
        {
            var subject = new ConnectionString(connectionString);

            subject.ApplicationName.Should().BeNull();
            subject.AuthMechanism.Should().BeNull();
            subject.AuthSource.Should().BeNull();
            subject.Connect.Should().Be(ClusterConnectionMode.Automatic);
            subject.ConnectTimeout.Should().Be(null);
            subject.DatabaseName.Should().BeNull();
            subject.FSync.Should().Be(null);
            subject.HeartbeatInterval.Should().NotHaveValue();
            subject.HeartbeatTimeout.Should().NotHaveValue();
            subject.Ipv6.Should().Be(null);
            subject.Journal.Should().Be(null);
            subject.MaxIdleTime.Should().Be(null);
            subject.MaxLifeTime.Should().Be(null);
            subject.MaxPoolSize.Should().Be(null);
            subject.MinPoolSize.Should().Be(null);
            subject.Password.Should().BeNull();
            subject.ReadConcernLevel.Should().BeNull();
            subject.ReadPreference.Should().BeNull();
            subject.ReadPreferenceTags.Should().BeNull();
            subject.ReplicaSet.Should().BeNull();
            subject.LocalThreshold.Should().Be(null);
            subject.SocketTimeout.Should().Be(null);
            subject.Ssl.Should().Be(null);
            subject.SslVerifyCertificate.Should().Be(null);
            subject.Username.Should().BeNull();
            subject.UuidRepresentation.Should().BeNull();
            subject.WaitQueueMultiple.Should().Be(null);
            subject.WaitQueueSize.Should().Be(null);
            subject.WaitQueueTimeout.Should().Be(null);
            subject.W.Should().BeNull();
            subject.WTimeout.Should().Be(null);
        }

        [Fact]
        public void When_everything_is_specified()
        {
            var connectionString = @"mongodb://user:pass@localhost1,localhost2:30000/test?" +
                "appname=app;" +
                "authMechanism=GSSAPI;" +
                "authMechanismProperties=CANONICALIZE_HOST_NAME:true;" +
                "authSource=admin;" +
                "connect=replicaSet;" +
                "connectTimeout=15ms;" +
                "fsync=true;" +
                "heartbeatInterval=1m;" +
                "heartbeatTimeout=2m;" +
                "ipv6=false;" +
                "j=true;" +
                "maxIdleTime=10ms;" +
                "maxLifeTime=5ms;" +
                "maxPoolSize=20;" +
                "minPoolSize=15;" +
                "readConcernLevel=majority;" +
                "readPreference=primary;" +
                "readPreferenceTags=dc:1;" +
                "replicaSet=funny;" +
                "localThreshold=50ms;" +
                "socketTimeout=40ms;" +
                "ssl=false;" +
                "sslVerifyCertificate=true;" +
                "uuidRepresentation=standard;" +
                "waitQueueMultiple=10;" +
                "waitQueueSize=30;" +
                "waitQueueTimeout=60ms;" +
                "w=4;" +
                "wtimeout=20ms";

            var subject = new ConnectionString(connectionString);

            subject.ApplicationName.Should().Be("app");
            subject.AuthMechanism.Should().Be("GSSAPI");
            subject.AuthMechanismProperties.Count.Should().Be(1);
            subject.AuthMechanismProperties["canonicalize_host_name"].Should().Be("true");
            subject.AuthSource.Should().Be("admin");
            subject.Connect.Should().Be(ClusterConnectionMode.ReplicaSet);
            subject.ConnectTimeout.Should().Be(TimeSpan.FromMilliseconds(15));
            subject.DatabaseName.Should().Be("test");
            subject.FSync.Should().BeTrue();
            subject.HeartbeatInterval.Should().Be(TimeSpan.FromMinutes(1));
            subject.HeartbeatTimeout.Should().Be(TimeSpan.FromMinutes(2));
            subject.Ipv6.Should().BeFalse();
            subject.Journal.Should().BeTrue();
            subject.MaxIdleTime.Should().Be(TimeSpan.FromMilliseconds(10));
            subject.MaxLifeTime.Should().Be(TimeSpan.FromMilliseconds(5));
            subject.MaxPoolSize.Should().Be(20);
            subject.MinPoolSize.Should().Be(15);
            subject.Password.Should().Be("pass");
            subject.ReadConcernLevel.Should().Be(ReadConcernLevel.Majority);
            subject.ReadPreference.Should().Be(ReadPreferenceMode.Primary);
            subject.ReadPreferenceTags.Single().Should().Be(new TagSet(new[] { new Tag("dc", "1") }));
            subject.ReplicaSet.Should().Be("funny");
            subject.LocalThreshold.Should().Be(TimeSpan.FromMilliseconds(50));
            subject.SocketTimeout.Should().Be(TimeSpan.FromMilliseconds(40));
            subject.Ssl.Should().BeFalse();
            subject.SslVerifyCertificate.Should().Be(true);
            subject.Username.Should().Be("user");
            subject.UuidRepresentation.Should().Be(GuidRepresentation.Standard);
            subject.WaitQueueMultiple.Should().Be(10);
            subject.WaitQueueSize.Should().Be(30);
            subject.WaitQueueTimeout.Should().Be(TimeSpan.FromMilliseconds(60));
            subject.W.Should().Be(WriteConcern.WValue.Parse("4"));
            subject.WTimeout.Should().Be(TimeSpan.FromMilliseconds(20));
        }

        [Theory]
        [InlineData("mongodb://localhost?appname=app1", "app1")]
        [InlineData("mongodb://localhost?appname=app2", "app2")]
        public void When_appname_is_specified(string connectionString, string applicationName)
        {
            var subject = new ConnectionString(connectionString);

            subject.ApplicationName.Should().Be(applicationName);
        }

        [Fact]
        public void When_appname_is_too_long()
        {
            var connectionString = $"mongodb://localhost?appname={new string('x', 129)}";

            var exception = Record.Exception(() => new ConnectionString(connectionString));

            exception.Should().BeOfType<MongoConfigurationException>();
        }

        [Theory]
        [InlineData("mongodb://localhost?authMechanism=GSSAPI", "GSSAPI")]
        [InlineData("mongodb://localhost?authMechanism=MONGODB-CR", "MONGODB-CR")]
        [InlineData("mongodb://localhost?authMechanism=PLAIN", "PLAIN")]
        [InlineData("mongodb://localhost?authMechanism=MONGODB-X509", "MONGODB-X509")]
        public void When_authMechanism_is_specified(string connectionString, string authMechanism)
        {
            var subject = new ConnectionString(connectionString);

            subject.AuthMechanism.Should().Be(authMechanism);
        }

        [Fact]
        public void When_authMechanismProperties_is_specified()
        {
            var connectionString = "mongodb://localhost?authMechanismProperties=ONE:1,TWO:2";
            var subject = new ConnectionString(connectionString);

            subject.AuthMechanismProperties.Count.Should().Be(2);
            subject.AuthMechanismProperties["one"].Should().Be("1");
            subject.AuthMechanismProperties["TWO"].Should().Be("2");
        }

        [Theory]
        [InlineData("mongodb://localhost?authSource=admin", "admin")]
        [InlineData("mongodb://localhost?authSource=awesome", "awesome")]
        public void When_authSource_is_specified(string connectionString, string authSource)
        {
            var subject = new ConnectionString(connectionString);

            subject.AuthSource.Should().Be(authSource);
        }

        [Theory]
        [InlineData("mongodb://localhost?connect=automatic", ClusterConnectionMode.Automatic)]
        [InlineData("mongodb://localhost?connect=direct", ClusterConnectionMode.Direct)]
        [InlineData("mongodb://localhost?connect=replicaSet", ClusterConnectionMode.ReplicaSet)]
        [InlineData("mongodb://localhost?connect=sharded", ClusterConnectionMode.Sharded)]
        [InlineData("mongodb://localhost?connect=ShardRouter", ClusterConnectionMode.Sharded)]
        [InlineData("mongodb://localhost?connect=sTaNdAlOnE", ClusterConnectionMode.Standalone)]
        public void When_connect_is_specified(string connectionString, ClusterConnectionMode connect)
        {
            var subject = new ConnectionString(connectionString);

            subject.Connect.Should().Be(connect);
        }

        [Theory]
        [InlineData("mongodb://localhost?connectTimeout=15ms", 15)]
        [InlineData("mongodb://localhost?connectTimeoutMS=15", 15)]
        [InlineData("mongodb://localhost?connectTimeout=15", 1000 * 15)]
        [InlineData("mongodb://localhost?connectTimeout=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?connectTimeout=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?connectTimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_connect_timeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.ConnectTimeout.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost/awesome", "awesome")]
        [InlineData("mongodb://localhost/awesome/", "awesome")]
        public void When_a_database_name_is_specified(string connectionString, string db)
        {
            var subject = new ConnectionString(connectionString);

            subject.DatabaseName.Should().Be(db);
        }

        [Theory]
        [InlineData("mongodb://localhost?fsync=true", true)]
        [InlineData("mongodb://localhost?fsync=false", false)]
        public void When_fsync_is_specified(string connectionString, bool fsync)
        {
            var subject = new ConnectionString(connectionString);

            subject.FSync.Should().Be(fsync);
        }

        [Theory]
        [InlineData("mongodb://localhost?gssapiServiceName=serviceName", "serviceName")]
        [InlineData("mongodb://localhost?gssapiServiceName=mongodb", "mongodb")]
        [InlineData("mongodb://localhost?authMechanismProperties=SERVICE_NAME:serviceName", "serviceName")]
        public void When_gssapiServiceName_is_specified(string connectionString, string gssapiServiceName)
        {
            var subject = new ConnectionString(connectionString);

            subject.AuthMechanismProperties["service_name"].Should().Be(gssapiServiceName);
        }

        [Theory]
        [InlineData("mongodb://localhost?heartbeatInterval=15ms", 15)]
        [InlineData("mongodb://localhost?heartbeatIntervalMS=15", 15)]
        [InlineData("mongodb://localhost?heartbeatInterval=15", 1000 * 15)]
        [InlineData("mongodb://localhost?heartbeatInterval=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?heartbeatInterval=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?heartbeatInterval=15h", 1000 * 60 * 60 * 15)]
        [InlineData("mongodb://localhost?heartbeatFrequency=15ms", 15)]
        [InlineData("mongodb://localhost?heartbeatFrequencyMS=15", 15)]
        [InlineData("mongodb://localhost?heartbeatFrequency=15", 1000 * 15)]
        [InlineData("mongodb://localhost?heartbeatFrequency=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?heartbeatFrequency=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?heartbeatFrequency=15h", 1000 * 60 * 60 * 15)]
        public void When_heartbeat_interval_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.HeartbeatInterval.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost?heartbeatTimeout=15ms", 15)]
        [InlineData("mongodb://localhost?heartbeatTimeoutMS=15", 15)]
        [InlineData("mongodb://localhost?heartbeatTimeout=15", 1000 * 15)]
        [InlineData("mongodb://localhost?heartbeatTimeout=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?heartbeatTimeout=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?heartbeatTimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_heartbeat_timeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.HeartbeatTimeout.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost?ipv6=true", true)]
        [InlineData("mongodb://localhost?ipv6=false", false)]
        public void When_ipv6_is_specified(string connectionString, bool ipv6)
        {
            var subject = new ConnectionString(connectionString);

            subject.Ipv6.Should().Be(ipv6);
        }

        [Theory]
        [InlineData("mongodb://localhost?j=true", true)]
        [InlineData("mongodb://localhost?j=false", false)]
        public void When_j_is_specified(string connectionString, bool j)
        {
            var subject = new ConnectionString(connectionString);

            subject.Journal.Should().Be(j);
        }

        [Theory]
        [InlineData("mongodb://localhost?maxIdleTime=15ms", 15)]
        [InlineData("mongodb://localhost?maxIdleTimeMS=15", 15)]
        [InlineData("mongodb://localhost?maxIdleTime=15", 1000 * 15)]
        [InlineData("mongodb://localhost?maxIdleTime=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?maxIdleTime=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?maxIdleTime=15h", 1000 * 60 * 60 * 15)]
        public void When_maxIdleTime_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.MaxIdleTime.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost?maxLifeTime=15ms", 15)]
        [InlineData("mongodb://localhost?maxLifeTimeMS=15", 15)]
        [InlineData("mongodb://localhost?maxLifeTime=15", 1000 * 15)]
        [InlineData("mongodb://localhost?maxLifeTime=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?maxLifeTime=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?maxLifeTime=15h", 1000 * 60 * 60 * 15)]
        public void When_maxLifeTime_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.MaxLifeTime.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost?maxPoolSize=-1", -1)]
        [InlineData("mongodb://localhost?maxPoolSize=0", 0)]
        [InlineData("mongodb://localhost?maxPoolSize=1", 1)]
        [InlineData("mongodb://localhost?maxPoolSize=20", 20)]
        public void When_maxPoolSize_is_specified(string connectionString, int maxPoolSize)
        {
            var subject = new ConnectionString(connectionString);

            subject.MaxPoolSize.Should().Be(maxPoolSize);
        }

        [Theory]
        [InlineData("mongodb://localhost?maxStaleness=15ms", 15)]
        [InlineData("mongodb://localhost?maxStalenessSeconds=0.015", 15)]
        [InlineData("mongodb://localhost?maxStaleness=15", 1000 * 15)]
        [InlineData("mongodb://localhost?maxStaleness=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?maxStaleness=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?maxStaleness=15h", 1000 * 60 * 60 * 15)]
        public void When_maxStaleness_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.MaxStaleness.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost")]
        [InlineData("mongodb://localhost?maxStalenessSeconds=-1")]
        [InlineData("mongodb://localhost?maxStaleness=-1")]
        [InlineData("mongodb://localhost?maxStaleness=-1s")]
        [InlineData("mongodb://localhost?maxStaleness=-1000ms")]
        public void When_no_maxStaleness_is_specified(string connectionString)
        {
            var subject = new ConnectionString(connectionString);

            subject.MaxStaleness.Should().NotHaveValue();
        }

        [Theory]
        [InlineData("mongodb://localhost?minPoolSize=-1", -1)]
        [InlineData("mongodb://localhost?minPoolSize=0", 0)]
        [InlineData("mongodb://localhost?minPoolSize=1", 1)]
        [InlineData("mongodb://localhost?minPoolSize=20", 20)]
        public void When_minPoolSize_is_specified(string connectionString, int minPoolSize)
        {
            var subject = new ConnectionString(connectionString);

            subject.MinPoolSize.Should().Be(minPoolSize);
        }

        [Theory]
        [InlineData("mongodb://a:yes@localhost", "yes")]
        [InlineData("mongodb://a:password@localhost", "password")]
        [InlineData("mongodb://a:@localhost", "")]
        public void When_password_is_specified(string connectionString, string password)
        {
            var subject = new ConnectionString(connectionString);

            subject.Password.Should().Be(password);
        }

        [Theory]
        [InlineData("mongodb://localhost?readConcernLevel=local", ReadConcernLevel.Local)]
        [InlineData("mongodb://localhost?readConcernLevel=majority", ReadConcernLevel.Majority)]
        public void When_readConcernLevel_is_specified(string connectionString, ReadConcernLevel readConcernLevel)
        {
            var subject = new ConnectionString(connectionString);

            subject.ReadConcernLevel.Should().Be(readConcernLevel);
        }

        [Theory]
        [InlineData("mongodb://localhost?readPreference=primary", ReadPreferenceMode.Primary)]
        [InlineData("mongodb://localhost?readPreference=primaryPreferred", ReadPreferenceMode.PrimaryPreferred)]
        [InlineData("mongodb://localhost?readPreference=secondaryPreferred", ReadPreferenceMode.SecondaryPreferred)]
        [InlineData("mongodb://localhost?readPreference=secondary", ReadPreferenceMode.Secondary)]
        [InlineData("mongodb://localhost?readPreference=nearest", ReadPreferenceMode.Nearest)]
        public void When_readPreference_is_specified(string connectionString, ReadPreferenceMode readPreference)
        {
            var subject = new ConnectionString(connectionString);

            subject.ReadPreference.Should().Be(readPreference);
        }

        [Fact]
        public void When_one_set_of_readPreferenceTags_is_specified()
        {
            var subject = new ConnectionString("mongodb://localhost?readPreferenceTags=dc:east,rack:1");

            var tagSet = new TagSet(new List<Tag>
            {
                new Tag("dc", "east"),
                new Tag("rack", "1")
            });

            subject.ReadPreferenceTags.Count.Should().Be(1);
            Assert.Equal(tagSet, subject.ReadPreferenceTags.Single());
        }

        [Fact]
        public void When_two_sets_of_readPreferenceTags_are_specified()
        {
            var subject = new ConnectionString("mongodb://localhost?readPreferenceTags=dc:east,rack:1&readPreferenceTags=dc:west,rack:2");

            var tagSet1 = new TagSet(new List<Tag>
            {
                new Tag("dc", "east"),
                new Tag("rack", "1")
            });

            var tagSet2 = new TagSet(new List<Tag>
            {
                new Tag("dc", "west"),
                new Tag("rack", "2")
            });

            subject.ReadPreferenceTags.Count.Should().Be(2);
            subject.ReadPreferenceTags[0].Should().Be(tagSet1);
            subject.ReadPreferenceTags[1].Should().Be(tagSet2);
        }

        [Theory]
        [InlineData("mongodb://localhost?replicaSet=yeah", "yeah")]
        public void When_replicaSet_is_specified(string connectionString, string replicaSet)
        {
            var subject = new ConnectionString(connectionString);

            subject.ReplicaSet.Should().Be(replicaSet);
        }

        [Theory]
        [InlineData("mongodb://localhost/?safe=false", 0)]
        [InlineData("mongodb://localhost/?w=1;safe=false", 0)]
        [InlineData("mongodb://localhost/?w=2;safe=false", 0)]
        [InlineData("mongodb://localhost/?w=mode;safe=false", 0)]
        [InlineData("mongodb://localhost/?safe=true", 1)]
        [InlineData("mongodb://localhost/?w=0;safe=true", 1)]
        [InlineData("mongodb://localhost/?w=2;safe=true", 2)]
        [InlineData("mongodb://localhost/?w=mode;safe=true", "mode")]
        public void When_safe_is_specified(string connectionString, object wobj)
        {
            var expectedW = (wobj == null) ? null : (wobj is int) ? (WriteConcern.WValue)(int)wobj : (string)wobj;
            var subject = new ConnectionString(connectionString);

            subject.W.Should().Be(expectedW);
        }

        [Theory]
        [InlineData("mongodb://localhost?localThreshold=15ms", 15)]
        [InlineData("mongodb://localhost?localThresholdMS=15", 15)]
        [InlineData("mongodb://localhost?localThreshold=15", 1000 * 15)]
        [InlineData("mongodb://localhost?localThreshold=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?localThreshold=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?localThreshold=15h", 1000 * 60 * 60 * 15)]
        public void When_localThreshold_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.LocalThreshold.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost?secondaryAcceptableLatency=15ms", 15)]
        [InlineData("mongodb://localhost?secondaryAcceptableLatencyMS=15", 15)]
        [InlineData("mongodb://localhost?secondaryAcceptableLatency=15", 1000 * 15)]
        [InlineData("mongodb://localhost?secondaryAcceptableLatency=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?secondaryAcceptableLatency=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?secondaryAcceptableLatency=15h", 1000 * 60 * 60 * 15)]
        public void When_secondaryAcceptableLatency_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.LocalThreshold.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost?serverSelectionTimeout=15ms", 15)]
        [InlineData("mongodb://localhost?serverSelectionTimeoutMS=15", 15)]
        [InlineData("mongodb://localhost?serverSelectionTimeout=15", 1000 * 15)]
        [InlineData("mongodb://localhost?serverSelectionTimeout=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?serverSelectionTimeout=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?serverSelectionTimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_serverSelectionTimeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.ServerSelectionTimeout.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost?socketTimeout=15ms", 15)]
        [InlineData("mongodb://localhost?socketTimeoutMS=15", 15)]
        [InlineData("mongodb://localhost?socketTimeout=15", 1000 * 15)]
        [InlineData("mongodb://localhost?socketTimeout=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?socketTimeout=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?socketTimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_socketTimeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.SocketTimeout.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost?ssl=true", true)]
        [InlineData("mongodb://localhost?ssl=false", false)]
        public void When_ssl_is_specified(string connectionString, bool ssl)
        {
            var subject = new ConnectionString(connectionString);

            subject.Ssl.Should().Be(ssl);
        }

        [Theory]
        [InlineData("mongodb://localhost?sslVerifyCertificate=true", true)]
        [InlineData("mongodb://localhost?sslVerifyCertificate=false", false)]
        public void When_sslVerifyCertificate_is_specified(string connectionString, bool sslVerifyCertificate)
        {
            var subject = new ConnectionString(connectionString);

            subject.SslVerifyCertificate.Should().Be(sslVerifyCertificate);
        }

        [Theory]
        [InlineData("mongodb://yes@localhost", "yes")]
        [InlineData("mongodb://username@localhost", "username")]
        public void When_username_is_specified(string connectionString, string username)
        {
            var subject = new ConnectionString(connectionString);

            subject.Username.Should().Be(username);
        }

        [Theory]
        [InlineData("mongodb://localhost?uuidRepresentation=standard", GuidRepresentation.Standard)]
        [InlineData("mongodb://localhost?guids=standard", GuidRepresentation.Standard)]
        [InlineData("mongodb://localhost?uuidRepresentation=csharpLegacy", GuidRepresentation.CSharpLegacy)]
        [InlineData("mongodb://localhost?guids=csharpLegacy", GuidRepresentation.CSharpLegacy)]
        [InlineData("mongodb://localhost?uuidRepresentation=javaLegacy", GuidRepresentation.JavaLegacy)]
        [InlineData("mongodb://localhost?guids=javaLegacy", GuidRepresentation.JavaLegacy)]
        [InlineData("mongodb://localhost?uuidRepresentation=pythonLegacy", GuidRepresentation.PythonLegacy)]
        [InlineData("mongodb://localhost?guids=pythonLegacy", GuidRepresentation.PythonLegacy)]
        public void When_uuidRepresentation_is_specified(string connectionString, GuidRepresentation representation)
        {
            var subject = new ConnectionString(connectionString);

            subject.UuidRepresentation.Should().Be(representation);
        }

        [Theory]
        [InlineData("mongodb://localhost?w=0", "0")]
        [InlineData("mongodb://localhost?w=1", "1")]
        [InlineData("mongodb://localhost?w=majority", "majority")]
        public void When_w_is_specified(string connectionString, string w)
        {
            var subject = new ConnectionString(connectionString);
            var expectedW = WriteConcern.WValue.Parse(w);

            subject.W.Should().Be(expectedW);
        }

        [Theory]
        [InlineData("mongodb://localhost?wtimeout=15ms", 15)]
        [InlineData("mongodb://localhost?wtimeoutMS=15", 15)]
        [InlineData("mongodb://localhost?wtimeout=15", 1000 * 15)]
        [InlineData("mongodb://localhost?wtimeout=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?wtimeout=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?wtimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_wtimeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.WTimeout.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Theory]
        [InlineData("mongodb://localhost?waitQueueMultiple=-1", -1)]
        [InlineData("mongodb://localhost?waitQueueMultiple=0", 0)]
        [InlineData("mongodb://localhost?waitQueueMultiple=1", 1)]
        [InlineData("mongodb://localhost?waitQueueMultiple=20", 20)]
        [InlineData("mongodb://localhost?waitQueueMultiple=2.3", 2.3)]
        public void When_waitQueueMultiple_is_specified(string connectionString, double waitQueueMultiple)
        {
            var subject = new ConnectionString(connectionString);

            subject.WaitQueueMultiple.Should().Be(waitQueueMultiple);
        }

        [Theory]
        [InlineData("mongodb://localhost?waitQueueSize=-1", -1)]
        [InlineData("mongodb://localhost?waitQueueSize=0", 0)]
        [InlineData("mongodb://localhost?waitQueueSize=1", 1)]
        [InlineData("mongodb://localhost?waitQueueSize=20", 20)]
        public void When_waitQueueSize_is_specified(string connectionString, int waitQueueSize)
        {
            var subject = new ConnectionString(connectionString);

            subject.WaitQueueSize.Should().Be(waitQueueSize);
        }

        [Theory]
        [InlineData("mongodb://localhost?waitQueueTimeout=15ms", 15)]
        [InlineData("mongodb://localhost?waitQueueTimeoutMS=15", 15)]
        [InlineData("mongodb://localhost?waitQueueTimeout=15", 1000 * 15)]
        [InlineData("mongodb://localhost?waitQueueTimeout=15s", 1000 * 15)]
        [InlineData("mongodb://localhost?waitQueueTimeout=15m", 1000 * 60 * 15)]
        [InlineData("mongodb://localhost?waitQueueTimeout=15h", 1000 * 60 * 60 * 15)]
        public void When_waitQueueTimeout_is_specified(string connectionString, int milliseconds)
        {
            var subject = new ConnectionString(connectionString);

            subject.WaitQueueTimeout.Should().Be(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Fact]
        public void When_uknown_options_exist()
        {
            var subject = new ConnectionString("mongodb://localhost?one=1;two=2");

            subject.AllUnknownOptionNames.Count().Should().Be(2);
            subject.AllUnknownOptionNames.Should().Contain("one");
            subject.AllUnknownOptionNames.Should().Contain("two");
            subject.GetOption("one").Should().Be("1");
            subject.GetOption("two").Should().Be("2");
        }
    }
}