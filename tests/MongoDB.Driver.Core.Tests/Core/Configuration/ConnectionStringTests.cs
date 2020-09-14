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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Compression;
using Xunit;

namespace MongoDB.Driver.Core.Configuration
{
    [Trait("Category", "ConnectionString")]
    public class ConnectionStringTests
    {
        [Theory]
        [InlineData("mongodb://localhost", true)]
        [InlineData("mongodb+srv://localhost", false)]
        public void constructor_should_initialize_isResolved(string connectionString, bool expectedIsResolved)
        {
            var subject = new ConnectionString(connectionString);

            subject.IsResolved.Should().Be(expectedIsResolved);
        }

        [Theory]
        [InlineData("mongodb://localhost", true)]
        [InlineData("mongodb+srv://localhost", false)]
        [InlineData("mongodb+srv://localhost", true)]
        public void constructor_with_isResolved_should_initialize_isResolved(string connectionString, bool isResolved)
        {
            var subject = new ConnectionString(connectionString, isResolved);

            subject.IsResolved.Should().Be(isResolved);
        }

        [Fact]
        public void constructor_should_throw_when_isResolved_is_invalid()
        {
            var exception = Record.Exception(() => new ConnectionString("mongodb://localhost", false));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("isResolved");
        }

        [Theory]
        [InlineData("mongodb://test5.test.build.10gen.cc", "mongodb://test5.test.build.10gen.cc", false)]
        [InlineData("mongodb://test5.test.build.10gen.cc", "mongodb://test5.test.build.10gen.cc", true)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", "mongodb://localhost.test.build.10gen.cc:27017/?replicaSet=repl0&authSource=thisDB&tls=true", false)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", "mongodb://localhost.test.build.10gen.cc:27017/?replicaSet=repl0&authSource=thisDB&tls=true", true)]
        public void Resolve_should_return_expected_result(string connectionString, string expectedResult, bool async)
        {
            var subject = new ConnectionString(connectionString);

            ConnectionString result;
            if (async)
            {
                result = subject.Resolve();
            }
            else
            {
                result = subject.ResolveAsync().GetAwaiter().GetResult();
            }

            result.IsResolved.Should().BeTrue();
            result.ToString().Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("mongodb://test5.test.build.10gen.cc", false, "mongodb://test5.test.build.10gen.cc", false)]
        [InlineData("mongodb://test5.test.build.10gen.cc", false, "mongodb://test5.test.build.10gen.cc", true)]
        [InlineData("mongodb://test5.test.build.10gen.cc", true, "mongodb://test5.test.build.10gen.cc", false)]
        [InlineData("mongodb://test5.test.build.10gen.cc", true, "mongodb://test5.test.build.10gen.cc", true)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", false, "mongodb+srv://test5.test.build.10gen.cc/?replicaSet=repl0&authSource=thisDB&tls=true", false)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", false, "mongodb+srv://test5.test.build.10gen.cc/?replicaSet=repl0&authSource=thisDB&tls=true", true)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", true, "mongodb://localhost.test.build.10gen.cc:27017/?replicaSet=repl0&authSource=thisDB&tls=true", false)]
        [InlineData("mongodb+srv://test5.test.build.10gen.cc", true, "mongodb://localhost.test.build.10gen.cc:27017/?replicaSet=repl0&authSource=thisDB&tls=true", true)]
        public void Resolve_with_resolveHosts_should_return_expected_result(string connectionString, bool resolveHosts, string expectedResult, bool async)
        {
            var subject = new ConnectionString(connectionString);

            ConnectionString result;
            if (async)
            {
                result = subject.Resolve(resolveHosts);
            }
            else
            {
                result = subject.ResolveAsync(resolveHosts).GetAwaiter().GetResult();
            }

            result.IsResolved.Should().BeTrue();
            result.ToString().Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("mongodb://test5.test.build.10gen.cc?tlsInsecure=true&tlsInsecure=false", true)]
        [InlineData("mongodb://test5.test.build.10gen.cc?tlsInsecure=false&tlsInsecure=true", true)]
        [InlineData("mongodb://test5.test.build.10gen.cc?tlsInsecure=true&tlsInsecure=true", false)]
        public void With_more_then_one_tlsInsecure(string connectionString, bool shouldThrow)
        {
            var exception = Record.Exception(() => { var _ = new ConnectionString(connectionString); });

            if (shouldThrow)
            {
                var e = exception.Should().BeOfType<MongoConfigurationException>().Subject;
                e.Message.Should().Be("tlsInsecure has already been configured with a different value.");
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [InlineData("mongodb://test5.test.build.10gen.cc?tls=true&tls=false", true)]
        [InlineData("mongodb://test5.test.build.10gen.cc?ssl=false&ssl=true", true)]
        [InlineData("mongodb://test5.test.build.10gen.cc?ssl=false&tls=true", true)]
        [InlineData("mongodb://test5.test.build.10gen.cc?tls=false&ssl=true", true)]
        [InlineData("mongodb://test5.test.build.10gen.cc?ssl=false&ssl=false", false)]
        [InlineData("mongodb://test5.test.build.10gen.cc?tls=false&tls=false", false)]
        [InlineData("mongodb://test5.test.build.10gen.cc?ssl=true&ssl=true", false)]
        [InlineData("mongodb://test5.test.build.10gen.cc?tls=true&tls=true", false)]
        public void With_more_then_one_tls_or_ssl(string connectionString, bool shouldThrow)
        {
            var exception = Record.Exception(() => { var _ = new ConnectionString(connectionString); });

            if (shouldThrow)
            {
                var e = exception.Should().BeOfType<MongoConfigurationException>().Subject;
                e.Message.Should().Be("tls has already been configured with a different value.");
            }
            else
            {
                exception.Should().BeNull();
            }
        }

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
            subject.Compressors.Should().BeEmpty();
#pragma warning disable CS0618 // Type or member is obsolete
            subject.Connect.Should().Be(ClusterConnectionMode.Automatic);
            subject.ConnectionModeSwitch.Should().Be(ConnectionModeSwitch.NotSet);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.ConnectTimeout.Should().Be(null);
            subject.DatabaseName.Should().BeNull();
            subject.DirectConnection.Should().NotHaveValue();
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
#pragma warning disable 618
            subject.Ssl.Should().Be(null);
            subject.SslVerifyCertificate.Should().Be(null);
#pragma warning restore 618
            subject.Tls.Should().Be(null);
            subject.TlsInsecure.Should().Be(null);
            subject.Username.Should().BeNull();
            subject.UuidRepresentation.Should().BeNull();
#pragma warning disable 618
            subject.WaitQueueMultiple.Should().Be(null);
            subject.WaitQueueSize.Should().Be(null);
#pragma warning restore 618
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
                "compressors=snappy,zlib;" +
                "zlibCompressionLevel=4;" +
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
                "retryReads=false;" +
                "retryWrites=true;" +
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
            var expectedCompressorTypes = new[] { CompressorType.Snappy, CompressorType.Zlib };
            subject.Compressors.Select(x => x.Type).Should().Equal(expectedCompressorTypes);
            subject.Compressors.Single(x => x.Type == CompressorType.Zlib).Properties["Level"].Should().Be(4);
#pragma warning disable CS0618 // Type or member is obsolete
            subject.Connect.Should().Be(ClusterConnectionMode.ReplicaSet);
            subject.ConnectionModeSwitch.Should().Be(ConnectionModeSwitch.UseConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete
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
            subject.RetryReads.Should().BeFalse();
            subject.RetryWrites.Should().BeTrue();
            subject.LocalThreshold.Should().Be(TimeSpan.FromMilliseconds(50));
            subject.SocketTimeout.Should().Be(TimeSpan.FromMilliseconds(40));
#pragma warning disable 618
            subject.Ssl.Should().BeFalse();
            subject.SslVerifyCertificate.Should().Be(true);
#pragma warning restore 618
            subject.Tls.Should().BeFalse();
            subject.TlsInsecure.Should().Be(false);
            subject.Username.Should().Be("user");
            subject.UuidRepresentation.Should().Be(GuidRepresentation.Standard);
#pragma warning disable 618
            subject.WaitQueueMultiple.Should().Be(10);
            subject.WaitQueueSize.Should().Be(30);
#pragma warning restore 618
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
        [InlineData("mongodb://localhost?compressors=zlib", CompressorType.Zlib)]
        public void When_compressor_is_specified(string connectionString, CompressorType compressor)
        {
            var subject = new ConnectionString(connectionString);

            subject.Compressors.Should().Contain(x => x.Type == compressor);
        }

        [Theory]
        [InlineData("mongodb://localhost?compressors=unsupported")]
        public void When_compressor_is_specified_with_unsupported_value_the_value_should_be_ignored(string connectionString)
        {
            var subject = new ConnectionString(connectionString);

            subject.Compressors.Should().BeEmpty();
        }

        [Theory]
        [InlineData("mongodb://nam!@#$%^&*())e:password@localhost", "mongodb://<hidden>@localhost")]
        [InlineData("://nam!@#$%^&*())e:password@loc", "://<hidden>@loc")]
        [InlineData("://nam!@#$%^&*())e@loc", "://<hidden>@loc")]
        [InlineData("mongodb://nameloc@", "mongodb://<hidden>@")]
        [InlineData("mongodb+srv://nameloc@", "mongodb+srv://<hidden>@")]
        [InlineData("ongodb://username:password@localhost/?replicaSet=@x", "ongodb://<hidden>@localhost/?replicaSet=@x")]
        public void When_connectionstring_invalid_security_data_should_be_protected(string connectionString, string protectedConnectionString)
        {
            var exception = Record.Exception(() => new ConnectionString(connectionString));
            var e = exception.Should().BeOfType<MongoConfigurationException>().Subject;
            e.Message.Should().StartWith($"The connection string '{protectedConnectionString}'");
        }

        [Theory]
#pragma warning disable CS0618 // Type or member is obsolete
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
            subject.ConnectionModeSwitch.Should().Be(ConnectionModeSwitch.UseConnectionMode);
        }
#pragma warning restore CS0618 // Type or member is obsolete

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
        [InlineData("mongodb://localhost/?directConnection=true&connect=automatic")]
        [InlineData("mongodb://localhost/?directConnection=false&connect=direct")]
        public void When_a_directConnection_and_connect_are_both_specified(string connectionString)
        {
            var exception = Record.Exception(() => new ConnectionString(connectionString));

            exception.Should().BeOfType<MongoConfigurationException>();
        }

        [Theory]
        [InlineData("mongodb://localhost/?directConnection=true&replicaSet=yeah", true)]
        [InlineData("mongodb://localhost/?directConnection=true", true)]
        [InlineData("mongodb://localhost/?directConnection=false&replicaSet=yeah", false)]
        [InlineData("mongodb://localhost/?directConnection=false", false)]
        public void When_a_directConnection_is_specified(string connectionString, bool directConnection)
        {
            var subject = new ConnectionString(connectionString);

#pragma warning disable CS0618 // Type or member is obsolete
            subject.ConnectionModeSwitch.Should().Be(ConnectionModeSwitch.UseDirectConnection);
#pragma warning restore CS0618 // Type or member is obsolete
            subject.DirectConnection.Should().Be(directConnection);
        }

        [Theory]
        [InlineData("mongodb+srv://localhost/?directConnection=false", false)]
        [InlineData("mongodb+srv://localhost/?directConnection=true", true)]
        public void When_a_directConnection_is_specified_with_a_srv_scheme(string connectionString, bool shouldThrow)
        {
            ConnectionString subject = null;
            var exception = Record.Exception(() => subject = new ConnectionString(connectionString));

            if (shouldThrow)
            {
                exception.Should().BeOfType<MongoConfigurationException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [InlineData("mongodb://localhost1,localhost2/?directConnection=false", false)]
        [InlineData("mongodb://localhost1,localhost2/?directConnection=true", true)]
        public void When_a_directConnection_is_specified_with_multiple_hosts(string connectionString, bool shouldThrow)
        {
            ConnectionString subject = null;
            var exception = Record.Exception(() => subject = new ConnectionString(connectionString));

            if (shouldThrow)
            {
                exception.Should().BeOfType<MongoConfigurationException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [InlineData("mongodb://localhost/?directConnection=true", "connect")]
        [InlineData("mongodb://localhost/?directConnection=false", "connect")]
        [InlineData("mongodb://localhost/?connect=direct", "directConnection")]
        [InlineData("mongodb://localhost/?connect=automatic", "directConnection")]
        public void When_not_expected_property_is_used(string connectionString, string propertyToCheck)
        {
            var subject = new ConnectionString(connectionString);

            Exception exception;
#pragma warning disable CS0618 // Type or member is obsolete
            switch (propertyToCheck)
            {
                case "connect": exception = Record.Exception(() => subject.Connect); break;
                case "directConnection": exception = Record.Exception(() => subject.DirectConnection); break;
                default: throw new Exception($"Not expected property {propertyToCheck}.");
            }
#pragma warning restore CS0618 // Type or member is obsolete
            exception.Should().BeOfType<InvalidOperationException>();
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
        [InlineData("mongodb://localhost?readConcernLevel=available", ReadConcernLevel.Available)]
        [InlineData("mongodb://localhost?readConcernLevel=linearizable", ReadConcernLevel.Linearizable)]
        [InlineData("mongodb://localhost?readConcernLevel=local", ReadConcernLevel.Local)]
        [InlineData("mongodb://localhost?readConcernLevel=majority", ReadConcernLevel.Majority)]
        [InlineData("mongodb://localhost?readConcernLevel=snapshot", ReadConcernLevel.Snapshot)]
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
        [InlineData("mongodb://localhost", null)]
        [InlineData("mongodb://localhost?retryReads=true", true)]
        [InlineData("mongodb://localhost?retryReads=false", false)]
        public void When_retryReads_is_specified(string connectionString, bool? retryReads)
        {
            var subject = new ConnectionString(connectionString);

            subject.RetryReads.Should().Be(retryReads);
        }

        [Theory]
        [InlineData("mongodb://localhost", null)]
        [InlineData("mongodb://localhost?retryWrites=true", true)]
        [InlineData("mongodb://localhost?retryWrites=false", false)]
        public void When_retryWrites_is_specified(string connectionString, bool? retryWrites)
        {
            var subject = new ConnectionString(connectionString);

            subject.RetryWrites.Should().Be(retryWrites);
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

#pragma warning disable 618
            subject.Ssl.Should().Be(ssl);
#pragma warning restore 618
        }

        [Theory]
        [InlineData("mongodb://localhost?sslVerifyCertificate=true", true)]
        [InlineData("mongodb://localhost?sslVerifyCertificate=false", false)]
        public void When_sslVerifyCertificate_is_specified(string connectionString, bool sslVerifyCertificate)
        {
            var subject = new ConnectionString(connectionString);

#pragma warning disable 618
            subject.SslVerifyCertificate.Should().Be(sslVerifyCertificate);
#pragma warning restore 618
        }

        [Theory]
        [InlineData("mongodb://localhost?tls=true", true)]
        [InlineData("mongodb://localhost?tls=false", false)]
        public void When_tls_is_specified(string connectionString, bool tls)
        {
            var subject = new ConnectionString(connectionString);

            subject.Tls.Should().Be(tls);
        }

        [Theory]
        [InlineData("mongodb://localhost?tlsInsecure=true", true)]
        [InlineData("mongodb://localhost?tlsInsecure=false", false)]
        public void When_tlsInsecure_is_specified(string connectionString, bool tlsInsecure)
        {
            var subject = new ConnectionString(connectionString);

            subject.TlsInsecure.Should().Be(tlsInsecure);
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

#pragma warning disable 618
            subject.WaitQueueMultiple.Should().Be(waitQueueMultiple);
#pragma warning restore 618
        }

        [Theory]
        [InlineData("mongodb://localhost?waitQueueSize=-1", -1)]
        [InlineData("mongodb://localhost?waitQueueSize=0", 0)]
        [InlineData("mongodb://localhost?waitQueueSize=1", 1)]
        [InlineData("mongodb://localhost?waitQueueSize=20", 20)]
        public void When_waitQueueSize_is_specified(string connectionString, int waitQueueSize)
        {
            var subject = new ConnectionString(connectionString);

#pragma warning disable 618
            subject.WaitQueueSize.Should().Be(waitQueueSize);
#pragma warning restore 618
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

        [Fact]
        public void When_multiple_hosts_are_provided_with_a_srv_scheme()
        {
            var connectionString = "mongodb+srv://localhost1,localhost2";

            var exception = Record.Exception(() => new ConnectionString(connectionString));

            exception.Should().BeOfType<MongoConfigurationException>();
        }

        [Fact]
        public void When_a_port_is_specified_with_a_srv_scheme()
        {
            var connectionString = "mongodb+srv://localhost1:53";

            var exception = Record.Exception(() => new ConnectionString(connectionString));

            exception.Should().BeOfType<MongoConfigurationException>();
        }

        [Fact]
        public void When_calling_resolve_on_a_srv_connection_string()
        {
            // NOTE: this requires SRV and TXT records in DNS as specified here:
            // https://github.com/mongodb/specifications/tree/master/source/initial-dns-seedlist-discovery
            var connectionString = "mongodb+srv://user%40GSSAPI.COM:password@test5.test.build.10gen.cc/funny?replicaSet=rs0";

            var subject = new ConnectionString(connectionString);

            var resolved = subject.Resolve();

            resolved.ToString().Should().Be("mongodb://user%40GSSAPI.COM:password@localhost.test.build.10gen.cc:27017/funny/?authSource=thisDB&replicaSet=rs0&tls=true");
        }

        [Fact]
        public async Task When_calling_resolve_async_on_a_srv_connection_string()
        {
            // NOTE: this requires SRV and TXT records in DNS as specified here:
            // https://github.com/mongodb/specifications/tree/master/source/initial-dns-seedlist-discovery
            var connectionString = "mongodb+srv://user%40GSSAPI.COM:password@test5.test.build.10gen.cc/funny?replicaSet=rs0";

            var subject = new ConnectionString(connectionString);

            var resolved = await subject.ResolveAsync();

            resolved.ToString().Should().Be("mongodb://user%40GSSAPI.COM:password@localhost.test.build.10gen.cc:27017/funny/?authSource=thisDB&replicaSet=rs0&tls=true");
        }

        [Fact]
        public void When_calling_resolve_on_a_native_connection_string()
        {
            var connectionString = "mongodb://localhost";

            var subject = new ConnectionString(connectionString);

            var resolved = subject.Resolve();

            resolved.Should().BeSameAs(subject);
        }
    }
}
